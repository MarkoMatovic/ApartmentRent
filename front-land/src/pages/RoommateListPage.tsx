import React, { useState } from 'react';
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
} from '@mui/material';
import { Add as AddIcon } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../shared/context/AuthContext';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { roommatesApi } from '../shared/api/roommates';
import { RoommateFilters } from '../shared/types/roommate';
import RoommateCard from '../components/Roommate/RoommateCard';

const RoommateListPage: React.FC = () => {
  const { t } = useTranslation(['common', 'roommates']);
  const navigate = useNavigate();
  const { isAuthenticated } = useAuth();
  
  const [filters, setFilters] = useState<RoommateFilters>({
    location: '',
    minBudget: undefined,
    maxBudget: undefined,
    smokingAllowed: undefined,
    petFriendly: undefined,
    lifestyle: undefined,
  });

  const { data: roommates, isLoading } = useQuery({
    queryKey: ['roommates', filters],
    queryFn: () => roommatesApi.getAll(filters),
  });

  const handleFilterChange = (field: keyof RoommateFilters, value: any) => {
    setFilters({ ...filters, [field]: value });
  };

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
          <Typography variant="body1" sx={{ mb: 2 }}>
            {roommates?.length || 0} {t('roommates:title')}
          </Typography>

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
              {roommates.map((roommate) => (
                <Grid item xs={12} sm={6} md={4} key={roommate.userId}>
                  <RoommateCard roommate={roommate} />
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
