import React, { useState, useMemo } from 'react';
import {
  Container,
  Grid,
  Typography,
  Box,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Paper,
  Skeleton,
  Button,
  ToggleButtonGroup,
  ToggleButton,
  Alert,
} from '@mui/material';
import { Add as AddIcon, Sort as SortIcon, AutoAwesome as AutoAwesomeIcon } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../shared/context/AuthContext';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { roommatesApi } from '../shared/api/roommates';
import { mlApi } from '../shared/api/analytics';
import { RoommateFilters } from '../shared/types/roommate';
import RoommateCard from '../components/Roommate/RoommateCard';

const RoommateListPage: React.FC = () => {
  const { t } = useTranslation(['common', 'roommates']);
  const navigate = useNavigate();
  const { isAuthenticated, user } = useAuth();

  const [filters, setFilters] = useState<RoommateFilters>({
    location: '',
    minBudget: undefined,
    maxBudget: undefined,
    smokingAllowed: undefined,
    petFriendly: undefined,
    lifestyle: undefined,
  });

  const [sortBy, setSortBy] = useState<'default' | 'bestMatch'>('default');

  const { data: roommates, isLoading } = useQuery({
    queryKey: ['roommates', filters],
    queryFn: () => roommatesApi.getAll(filters),
  });

  // Fetch match scores if user is authenticated and has a roommate profile
  const { data: matchScores, isLoading: matchesLoading } = useQuery({
    queryKey: ['roommate-matches', user?.userId],
    queryFn: () => mlApi.getRoommateMatches(user!.userId, 50),
    enabled: isAuthenticated && !!user?.userId && sortBy === 'bestMatch',
  });

  const handleFilterChange = (field: keyof RoommateFilters, value: any) => {
    setFilters({ ...filters, [field]: value });
  };

  // Merge roommates with match scores
  const roommatesWithScores = useMemo(() => {
    if (!roommates) return [];

    if (sortBy === 'bestMatch' && matchScores) {
      const scoreMap = new Map(matchScores.map(m => [m.roommateId, m.matchPercentage]));

      return roommates
        .map(rm => ({
          ...rm,
          matchScore: scoreMap.get(rm.roommateId),
        }))
        .sort((a, b) => (b.matchScore || 0) - (a.matchScore || 0));
    }

    return roommates;
  }, [roommates, matchScores, sortBy]);

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" component="h1">
          {t('roommates:title')}
        </Typography>
        {isAuthenticated && (
          <Button
            variant="contained"
            color="secondary"
            startIcon={<AddIcon />}
            onClick={() => navigate('/roommates/create')}
          >
            {t('roommates:createProfile')}
          </Button>
        )}
      </Box>

      <Grid container spacing={3}>
        {/* Filters Sidebar */}
        <Grid item xs={12} md={3}>
          <Paper sx={{ p: 3, position: 'sticky', top: 100 }}>
            <Typography variant="h6" gutterBottom>
              {t('roommates:filters')}
            </Typography>

            <TextField
              fullWidth
              label={t('roommates:location')}
              value={filters.location || ''}
              onChange={(e) => handleFilterChange('location', e.target.value || undefined)}
              margin="normal"
              size="small"
            />

            <TextField
              fullWidth
              label={`${t('roommates:minBudget')} (€)`}
              type="number"
              value={filters.minBudget || ''}
              onChange={(e) => handleFilterChange('minBudget', e.target.value ? parseFloat(e.target.value) : undefined)}
              margin="normal"
              size="small"
            />

            <TextField
              fullWidth
              label={`${t('roommates:maxBudget')} (€)`}
              type="number"
              value={filters.maxBudget || ''}
              onChange={(e) => handleFilterChange('maxBudget', e.target.value ? parseFloat(e.target.value) : undefined)}
              margin="normal"
              size="small"
            />

            <FormControl fullWidth margin="normal" size="small">
              <InputLabel>{t('roommates:smokingAllowed')}</InputLabel>
              <Select
                value={filters.smokingAllowed === undefined ? '' : filters.smokingAllowed.toString()}
                onChange={(e) => handleFilterChange('smokingAllowed', e.target.value === '' ? undefined : e.target.value === 'true')}
                label={t('roommates:smokingAllowed')}
              >
                <MenuItem value="">All</MenuItem>
                <MenuItem value="true">Yes</MenuItem>
                <MenuItem value="false">No</MenuItem>
              </Select>
            </FormControl>

            <FormControl fullWidth margin="normal" size="small">
              <InputLabel>{t('roommates:petFriendly')}</InputLabel>
              <Select
                value={filters.petFriendly === undefined ? '' : filters.petFriendly.toString()}
                onChange={(e) => handleFilterChange('petFriendly', e.target.value === '' ? undefined : e.target.value === 'true')}
                label={t('roommates:petFriendly')}
              >
                <MenuItem value="">All</MenuItem>
                <MenuItem value="true">Yes</MenuItem>
                <MenuItem value="false">No</MenuItem>
              </Select>
            </FormControl>

            <FormControl fullWidth margin="normal" size="small">
              <InputLabel>{t('roommates:lifestyle')}</InputLabel>
              <Select
                value={filters.lifestyle || ''}
                onChange={(e) => handleFilterChange('lifestyle', e.target.value || undefined)}
                label={t('roommates:lifestyle')}
              >
                <MenuItem value="">All</MenuItem>
                <MenuItem value="quiet">{t('roommates:quiet')}</MenuItem>
                <MenuItem value="social">{t('roommates:social')}</MenuItem>
                <MenuItem value="mixed">{t('roommates:mixed')}</MenuItem>
              </Select>
            </FormControl>
          </Paper>
        </Grid>

        {/* Roommate List */}
        <Grid item xs={12} md={9}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
            <Typography variant="body1">
              {roommates?.length || 0} {t('roommates:title')}
            </Typography>

            {isAuthenticated && (
              <ToggleButtonGroup
                value={sortBy}
                exclusive
                onChange={(_, value) => value && setSortBy(value)}
                size="small"
              >
                <ToggleButton value="default">
                  <SortIcon sx={{ mr: 1 }} fontSize="small" />
                  Default
                </ToggleButton>
                <ToggleButton value="bestMatch">
                  <AutoAwesomeIcon sx={{ mr: 1 }} fontSize="small" />
                  Best Matches
                </ToggleButton>
              </ToggleButtonGroup>
            )}
          </Box>

          {sortBy === 'bestMatch' && matchesLoading && (
            <Alert severity="info" sx={{ mb: 2 }}>
              Calculating compatibility scores...
            </Alert>
          )}

          {isLoading ? (
            <Grid container spacing={3}>
              {[1, 2, 3].map((i) => (
                <Grid item xs={12} sm={6} md={4} key={i}>
                  <Skeleton variant="rectangular" height={400} />
                </Grid>
              ))}
            </Grid>
          ) : !roommates || roommates.length === 0 ? (
            <Box sx={{ textAlign: 'center', py: 8 }}>
              <Typography variant="h6" color="text.secondary">
                {t('roommates:noRoommates')}
              </Typography>
            </Box>
          ) : (
            <Grid container spacing={3}>
              {roommatesWithScores.map((roommate) => (
                <Grid item xs={12} sm={6} md={4} key={roommate.userId}>
                  <RoommateCard
                    roommate={roommate}
                    matchScore={sortBy === 'bestMatch' ? roommate.matchScore : undefined}
                  />
                </Grid>
              ))}
            </Grid>
          )}
        </Grid>
      </Grid>
    </Container>
  );
};

export default RoommateListPage;
