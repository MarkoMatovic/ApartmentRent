import React, { useState, useEffect } from 'react';
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
  Button,
  Card,
  CardMedia,
  CardContent,
  CardActions,
  Chip,
  Skeleton,
} from '@mui/material';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { apartmentsApi } from '../shared/api/apartments';
import { ApartmentDto, ApartmentType } from '../shared/types/apartment';
import { Home as HomeIcon, LocationOn as LocationIcon } from '@mui/icons-material';

const ApartmentListPage: React.FC = () => {
  const { t } = useTranslation(['common', 'apartments']);
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  
  const [filters, setFilters] = useState({
    city: searchParams.get('location') || '',
    minPrice: '',
    maxPrice: '',
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
              value={filters.minPrice}
              onChange={(e) => handleFilterChange('minPrice', e.target.value)}
              margin="normal"
              size="small"
            />
            
            <TextField
              fullWidth
              label={`${t('apartments:price')} (Max)`}
              type="number"
              value={filters.maxPrice}
              onChange={(e) => handleFilterChange('maxPrice', e.target.value)}
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
                  <Card
                    sx={{
                      height: '100%',
                      display: 'flex',
                      flexDirection: 'column',
                      cursor: 'pointer',
                      '&:hover': { boxShadow: 4 },
                    }}
                    onClick={() => navigate(`/apartments/${apartment.apartmentId}`)}
                  >
                    <CardMedia
                      component="div"
                      sx={{
                        height: 200,
                        bgcolor: 'grey.300',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                      }}
                    >
                      <HomeIcon sx={{ fontSize: 60, color: 'grey.500' }} />
                    </CardMedia>
                    <CardContent sx={{ flexGrow: 1 }}>
                      <Typography variant="h6" component="h2" gutterBottom>
                        {apartment.title}
                      </Typography>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
                        <LocationIcon fontSize="small" color="action" />
                        <Typography variant="body2" color="text.secondary">
                          {apartment.address}, {apartment.city}
                        </Typography>
                      </Box>
                      <Typography variant="h5" color="primary" sx={{ mt: 1 }}>
                        €{apartment.rent}/mo
                      </Typography>
                      <Box sx={{ mt: 1, display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                        {apartment.isFurnished && (
                          <Chip label={t('apartments:furnished')} size="small" />
                        )}
                        {apartment.isImmediatelyAvailable && (
                          <Chip label={t('apartments:immediatelyAvailable')} size="small" color="success" />
                        )}
                      </Box>
                    </CardContent>
                    <CardActions>
                      <Button size="small" color="secondary">
                        {t('view')}
                      </Button>
                    </CardActions>
                  </Card>
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

