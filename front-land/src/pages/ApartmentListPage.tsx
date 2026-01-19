import React, { useState } from 'react';
import {
  Container,
  Grid,
  Typography,
  Box,
  TextField,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Skeleton,
} from '@mui/material';
import { useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { apartmentsApi } from '../shared/api/apartments';
import ApartmentCard from '../components/Apartment/ApartmentCard';

const ApartmentListPage: React.FC = () => {
  const { t } = useTranslation(['common', 'apartments']);
  const [searchParams] = useSearchParams();
  
  const [filters, setFilters] = useState({
    city: searchParams.get('location') || '',
    minRent: '',
    maxRent: '',
    numberOfRooms: '',
    apartmentType: '',
    isFurnished: '',
  });

  const [sortBy, setSortBy] = useState('date');

  const { data: apartments, isLoading } = useQuery({
    queryKey: ['apartments', filters],
    queryFn: () => apartmentsApi.getAll(filters as any),
  });

  const handleFilterChange = (field: string, value: any) => {
    setFilters({ ...filters, [field]: value });
  };

  const handleSortChange = (value: string) => {
    setSortBy(value);
    // Implement sorting logic
  };

  const sortedApartments = apartments ? [...apartments].sort((a, b) => {
    switch (sortBy) {
      case 'price-asc':
        return a.rent - b.rent;
      case 'price-desc':
        return b.rent - a.rent;
      default:
        return 0;
    }
  }) : [];

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Typography variant="h4" component="h1" gutterBottom>
        {t('apartments:title')}
      </Typography>

      <Grid container spacing={3}>
        {/* Filters Sidebar */}
        <Grid item xs={12} md={3}>
          <Box sx={{ position: 'sticky', top: 100 }}>
            <Typography variant="h6" gutterBottom>
              {t('apartments:filters')}
            </Typography>
            
            <TextField
              fullWidth
              label={t('apartments:location')}
              value={filters.city}
              onChange={(e) => handleFilterChange('city', e.target.value)}
              margin="normal"
              size="small"
            />
            
            <TextField
              fullWidth
              label={`${t('apartments:price')} (Min)`}
              type="number"
              value={filters.minRent}
              onChange={(e) => handleFilterChange('minRent', e.target.value)}
              margin="normal"
              size="small"
            />
            
            <TextField
              fullWidth
              label={`${t('apartments:price')} (Max)`}
              type="number"
              value={filters.maxRent}
              onChange={(e) => handleFilterChange('maxRent', e.target.value)}
              margin="normal"
              size="small"
            />
            
            <FormControl fullWidth margin="normal" size="small">
              <InputLabel>{t('apartments:rooms')}</InputLabel>
              <Select
                value={filters.numberOfRooms}
                onChange={(e) => handleFilterChange('numberOfRooms', e.target.value)}
                label={t('apartments:rooms')}
              >
                <MenuItem value="">All</MenuItem>
                <MenuItem value="1">1</MenuItem>
                <MenuItem value="2">2</MenuItem>
                <MenuItem value="3">3</MenuItem>
                <MenuItem value="4">4+</MenuItem>
              </Select>
            </FormControl>
          </Box>
        </Grid>

        {/* Apartment List */}
        <Grid item xs={12} md={9}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
            <Typography variant="body1">
              {apartments?.length || 0} {t('apartments:title')}
            </Typography>
            <FormControl size="small" sx={{ minWidth: 200 }}>
              <InputLabel>{t('apartments:sortBy')}</InputLabel>
              <Select value={sortBy} onChange={(e) => handleSortChange(e.target.value)} label={t('apartments:sortBy')}>
                <MenuItem value="date">{t('apartments:sortDateNewest')}</MenuItem>
                <MenuItem value="price-asc">{t('apartments:sortPriceAsc')}</MenuItem>
                <MenuItem value="price-desc">{t('apartments:sortPriceDesc')}</MenuItem>
              </Select>
            </FormControl>
          </Box>

          {isLoading ? (
            <Grid container spacing={3}>
              {[1, 2, 3].map((i) => (
                <Grid item xs={12} sm={6} key={i}>
                  <Skeleton variant="rectangular" height={200} />
                  <Skeleton height={40} />
                  <Skeleton height={20} />
                </Grid>
              ))}
            </Grid>
          ) : sortedApartments.length === 0 ? (
            <Box sx={{ textAlign: 'center', py: 8 }}>
              <Typography variant="h6" color="text.secondary">
                {t('apartments:noApartments')}
              </Typography>
            </Box>
          ) : (
            <Grid container spacing={3}>
              {sortedApartments.map((apartment) => (
                <Grid item xs={12} sm={6} key={apartment.apartmentId}>
                  <ApartmentCard apartment={apartment} />
                </Grid>
              ))}
            </Grid>
          )}
        </Grid>
      </Grid>
    </Container>
  );
};

export default ApartmentListPage;

