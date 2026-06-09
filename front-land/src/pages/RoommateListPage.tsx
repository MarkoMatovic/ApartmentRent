import React, { useState, useMemo, useEffect } from 'react';
import {
  Container, Grid, Typography, Box, TextField, Paper,
  Button, ToggleButtonGroup, ToggleButton, Chip, Collapse,
  Divider, Skeleton, Alert,
} from '@mui/material';
import {
  Add as AddIcon, AutoAwesome as AIIcon,
  FilterList as FilterIcon, Close as CloseIcon,
  SmokingRooms as SmokingIcon, SmokeFree as NoSmokingIcon,
  Pets as PetsIcon, MusicNote as MusicIcon, Search as SearchIcon,
} from '@mui/icons-material';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useAuth } from '../shared/context/AuthContext';
import { useQuery } from '@tanstack/react-query';
import { roommatesApi } from '../shared/api/roommates';
import { mlApi, analyticsApi } from '../shared/api/analytics';
import {
  RoommateFilters, Roommate,
  LIFESTYLE_ICONS, LIFESTYLE_LABELS,
  SCHEDULE_ICONS, SCHEDULE_LABELS,
} from '../shared/types/roommate';
import RoommateCard from '../components/Roommate/RoommateCard';

const FilterChip: React.FC<{
  label: string; active: boolean; onClick: () => void;
}> = ({ label, active, onClick }) => (
  <Chip
    label={label} onClick={onClick} clickable size="small"
    color={active ? 'primary' : 'default'}
    variant={active ? 'filled' : 'outlined'}
    sx={{ fontWeight: active ? 700 : 400, transition: 'all 0.15s' }}
  />
);

const RoommateListPage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { isAuthenticated, user } = useAuth();
  const [showFilters, setShowFilters] = useState(true);
  const [sortBy, setSortBy] = useState<'default' | 'bestMatch' | 'soonest'>('default');

  const [filters, setFilters] = useState<RoommateFilters>({
    location: searchParams.get('location') || '',
    minBudget: undefined, maxBudget: undefined,
    smokingAllowed: undefined, petFriendly: undefined, lifestyle: undefined,
  });

  const set = (field: keyof RoommateFilters, value: any) =>
    setFilters(prev => ({ ...prev, [field]: value }));

  const toggleBool = (field: keyof RoommateFilters, value: boolean) =>
    setFilters(prev => ({ ...prev, [field]: prev[field] === value ? undefined : value }));

  const toggleString = (field: keyof RoommateFilters, value: string) =>
    setFilters(prev => ({ ...prev, [field]: prev[field] === value ? undefined : value }));

  const activeCount = [
    filters.location, filters.minBudget, filters.maxBudget,
    filters.smokingAllowed, filters.petFriendly, filters.lifestyle,
    filters.workSchedule,
  ].filter(v => v !== undefined && v !== '' && v !== null).length;

  const { data: roommates, isLoading } = useQuery({
    queryKey: ['roommates', filters],
    queryFn: () => roommatesApi.getAll(filters),
  });

  const { data: myCard } = useQuery({
    queryKey: ['roommate-my', user?.userId],
    queryFn: () => roommatesApi.getByUserId(user!.userId),
    enabled: isAuthenticated && !!user?.userId,
    retry: false,
  });

  const { data: matchScores, isLoading: matchesLoading } = useQuery({
    queryKey: ['roommate-matches', user?.userId],
    queryFn: () => mlApi.getRoommateMatches(user!.userId, 100),
    enabled: isAuthenticated && !!user?.userId && sortBy === 'bestMatch',
  });

  useEffect(() => {
    if (!filters.location?.trim()) return;
    const t = setTimeout(() => analyticsApi.trackEvent('RoommateSearch', 'Roommates'), 2000);
    return () => clearTimeout(t);
  }, [filters.location]);

  const sorted = useMemo(() => {
    if (!roommates) return [];
    let list: (Roommate & { matchScore?: number })[] = [...roommates];
    if (sortBy === 'bestMatch' && matchScores) {
      const map = new Map((matchScores as any[]).map(m => [m.roommateId, m.matchPercentage]));
      list = list.map(r => ({ ...r, matchScore: map.get(r.roommateId) }))
        .sort((a, b) => (b.matchScore || 0) - (a.matchScore || 0));
    } else if (sortBy === 'soonest') {
      list.sort((a, b) => {
        if (!a.availableFrom) return 1;
        if (!b.availableFrom) return -1;
        return new Date(a.availableFrom).getTime() - new Date(b.availableFrom).getTime();
      });
    }
    return list;
  }, [roommates, matchScores, sortBy]);

  return (
    <Container maxWidth="xl" sx={{ py: 4 }}>
      {/* Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3, flexWrap: 'wrap', gap: 1 }}>
        <Box>
          <Typography variant="h4" fontWeight="bold">Cimeri</Typography>
          <Typography variant="body2" color="text.secondary">Pronađi idealnog cimera u Srbiji</Typography>
        </Box>
        {isAuthenticated && (
          <Button variant="contained" color="secondary"
            startIcon={myCard ? undefined : <AddIcon />}
            onClick={() => navigate('/roommates/create')}>
            {myCard ? 'Uredi profil' : 'Kreiraj profil'}
          </Button>
        )}
      </Box>

      {/* Search + filter bar */}
      <Paper sx={{ p: 2, mb: 3, borderRadius: 3 }}>
        <Box sx={{ display: 'flex', gap: 1.5, alignItems: 'center', flexWrap: 'wrap' }}>
          <TextField
            placeholder="Grad, opština ili kvart..."
            value={filters.location || ''}
            onChange={e => set('location', e.target.value || undefined)}
            size="small"
            InputProps={{ startAdornment: <SearchIcon sx={{ mr: 1, color: 'text.secondary', fontSize: 18 }} /> }}
            sx={{ minWidth: 220, flex: 1 }}
          />
          <TextField placeholder="Budžet od (€)" type="number"
            value={filters.minBudget || ''}
            onChange={e => set('minBudget', e.target.value ? parseFloat(e.target.value) : undefined)}
            size="small" sx={{ width: 130 }}
          />
          <TextField placeholder="Budžet do (€)" type="number"
            value={filters.maxBudget || ''}
            onChange={e => set('maxBudget', e.target.value ? parseFloat(e.target.value) : undefined)}
            size="small" sx={{ width: 130 }}
          />
          <Button variant={showFilters ? 'contained' : 'outlined'}
            startIcon={<FilterIcon />} onClick={() => setShowFilters(v => !v)} size="small"
            endIcon={activeCount > 0
              ? <Chip label={activeCount} size="small" color="error" sx={{ height: 18, fontSize: '0.65rem', ml: -0.5 }} />
              : undefined}>
            Filteri
          </Button>
          {activeCount > 0 && (
            <Button size="small" color="error" startIcon={<CloseIcon />}
              onClick={() => setFilters({ location: '', minBudget: undefined, maxBudget: undefined, smokingAllowed: undefined, petFriendly: undefined, lifestyle: undefined, workSchedule: undefined })}>
              Resetuj
            </Button>
          )}
        </Box>

        <Collapse in={showFilters}>
          <Divider sx={{ my: 1.5 }} />
          <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', alignItems: 'center' }}>
            <Typography variant="caption" color="text.secondary" sx={{ mr: 0.3 }}>Pušenje:</Typography>
            <FilterChip label="🚬 Pušač" active={filters.smokingAllowed === true}
              onClick={() => toggleBool('smokingAllowed', true)} />
            <FilterChip label="🚭 Nepušač" active={filters.smokingAllowed === false}
              onClick={() => toggleBool('smokingAllowed', false)} />

            <Divider orientation="vertical" flexItem sx={{ mx: 0.5 }} />
            <Typography variant="caption" color="text.secondary" sx={{ mr: 0.3 }}>Stil:</Typography>
            {(['quiet', 'social', 'mixed'] as const).map(ls => (
              <FilterChip key={ls}
                label={`${LIFESTYLE_ICONS[ls]} ${LIFESTYLE_LABELS[ls]}`}
                active={filters.lifestyle === ls}
                onClick={() => toggleString('lifestyle', ls)} />
            ))}

            <Divider orientation="vertical" flexItem sx={{ mx: 0.5 }} />
            <FilterChip label="🐾 Ljubimci OK" active={filters.petFriendly === true}
              onClick={() => toggleBool('petFriendly', true)} />

            <Divider orientation="vertical" flexItem sx={{ mx: 0.5 }} />
            <Typography variant="caption" color="text.secondary" sx={{ mr: 0.3 }}>Ritam:</Typography>
            {([1, 2, 3] as const).map(ws => (
              <FilterChip key={ws}
                label={`${SCHEDULE_ICONS[ws]} ${SCHEDULE_LABELS[ws]}`}
                active={filters.workSchedule === ws}
                onClick={() => setFilters(prev => ({ ...prev, workSchedule: prev.workSchedule === ws ? undefined : ws }))} />
            ))}
          </Box>
        </Collapse>
      </Paper>

      {/* Results header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2, flexWrap: 'wrap', gap: 1 }}>
        <Typography variant="body1" color="text.secondary">
          {isLoading ? 'Učitava...' : `${sorted.length} ${sorted.length === 1 ? 'profil' : 'profila'}`}
        </Typography>
        <ToggleButtonGroup value={sortBy} exclusive size="small"
          onChange={(_, v) => v && setSortBy(v)}>
          <ToggleButton value="default">Najnoviji</ToggleButton>
          <ToggleButton value="soonest">📅 Useljenje</ToggleButton>
          {isAuthenticated && (
            <ToggleButton value="bestMatch" disabled={matchesLoading}>
              <AIIcon sx={{ mr: 0.5, fontSize: 16 }} />
              {matchesLoading ? 'Računam...' : 'AI poklapanje'}
            </ToggleButton>
          )}
        </ToggleButtonGroup>
      </Box>

      {sortBy === 'bestMatch' && !matchesLoading && !myCard && (
        <Alert severity="info" sx={{ mb: 2 }}>
          Kreiraj roommate profil da vidiš AI poklapanje s drugima.{' '}
          <Button size="small" onClick={() => navigate('/roommates/create')}>Kreiraj profil</Button>
        </Alert>
      )}

      {/* My card at top */}
      {myCard && (
        <Box sx={{ mb: 3 }}>
          <Typography variant="overline" color="text.secondary">Moj profil</Typography>
          <Grid container spacing={2.5} sx={{ mt: 0 }}>
            <Grid item xs={12} sm={6} md={4} lg={3}>
              <RoommateCard roommate={myCard} isOwn />
            </Grid>
          </Grid>
          <Divider sx={{ mt: 3, mb: 2 }} />
        </Box>
      )}

      {/* Grid */}
      {isLoading ? (
        <Grid container spacing={2.5}>
          {[1,2,3,4,5,6].map(i => (
            <Grid item xs={12} sm={6} md={4} lg={3} key={i}>
              <Skeleton variant="rectangular" height={380} sx={{ borderRadius: 3 }} />
            </Grid>
          ))}
        </Grid>
      ) : sorted.length === 0 ? (
        <Box sx={{ textAlign: 'center', py: 10 }}>
          <Typography variant="h6" color="text.secondary" gutterBottom>
            Nema rezultata za ove filtere
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Pokušaj sa manje filtera
          </Typography>
          {activeCount > 0 && (
            <Button variant="outlined" onClick={() => setFilters({})}>Ukloni sve filtere</Button>
          )}
        </Box>
      ) : (
        <Grid container spacing={2.5}>
          {sorted.map(roommate => (
            <Grid item xs={12} sm={6} md={4} lg={3} key={roommate.roommateId}>
              <RoommateCard
                roommate={roommate}
                matchScore={sortBy === 'bestMatch' ? roommate.matchScore : undefined}
              />
            </Grid>
          ))}
        </Grid>
      )}
    </Container>
  );
};

export default RoommateListPage;
