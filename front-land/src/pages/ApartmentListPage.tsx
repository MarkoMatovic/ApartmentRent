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
  Skeleton,
  Button,
  Drawer,
  IconButton,
  useTheme,
  useMediaQuery,
} from '@mui/material';
import { FilterList as FilterListIcon, Close as CloseIcon } from '@mui/icons-material';
import { useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { apartmentsApi } from '../shared/api/apartments';
import ApartmentCard from '../components/Apartment/ApartmentCard';

const ApartmentListPage: React.FC = () => {
  const { t } = useTranslation(['common', 'apartments']);
  const [searchParams] = useSearchParams();
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const [filterDrawerOpen, setFilterDrawerOpen] = useState(false);

  const [filters, setFilters] = useState({
    listingType: '',
    city: searchParams.get('location') || '',
    minRent: '',
    maxRent: '',
    numberOfRooms: '',
    apartmentType: '',
    isFurnished: '',
    hasParking: '',
    hasBalcony: '',
    isPetFriendly: '',
    isSmokingAllowed: '',
    isImmediatelyAvailable: false,
    availableFrom: '',
    sortBy: 'date',
    sortOrder: 'desc',
  });

  const [page, setPage] = useState(1);
  const pageSize = 20;

  // Scroll to top when page changes
  useEffect(() => {
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }, [page]);

  const { data: response, isLoading } = useQuery({
    queryKey: ['apartments', filters],
    queryFn: () => {
      const filterParams: any = {
        page: page,
        pageSize: pageSize,
      };

      // ListingType filter
      if (filters.listingType && filters.listingType !== '') {
        filterParams.listingType = Number(filters.listingType);
      }

      // Other filters
      if (filters.city) filterParams.city = filters.city;
      if (filters.minRent) filterParams.minRent = Number(filters.minRent);
      if (filters.maxRent) filterParams.maxRent = Number(filters.maxRent);
      if (filters.numberOfRooms) filterParams.numberOfRooms = Number(filters.numberOfRooms);
      if (filters.apartmentType) filterParams.apartmentType = Number(filters.apartmentType);
      if (filters.apartmentType) filterParams.apartmentType = Number(filters.apartmentType);

      // Boolean filters
      if (filters.isFurnished) filterParams.isFurnished = filters.isFurnished === 'true';
      if (filters.hasParking) filterParams.hasParking = filters.hasParking === 'true';
      if (filters.hasBalcony) filterParams.hasBalcony = filters.hasBalcony === 'true';
      if (filters.isPetFriendly) filterParams.isPetFriendly = filters.isPetFriendly === 'true';
      if (filters.isSmokingAllowed) filterParams.isSmokingAllowed = filters.isSmokingAllowed === 'true';
      if (filters.isImmediatelyAvailable) filterParams.isImmediatelyAvailable = true;
      if (filters.availableFrom) filterParams.availableFrom = filters.availableFrom;

      // Sorting
      if (filters.sortBy) filterParams.sortBy = filters.sortBy;
      if (filters.sortOrder) filterParams.sortOrder = filters.sortOrder;

      return apartmentsApi.getAll(filterParams);
    },
    staleTime: 0,
  });

  const apartments = response?.items || [];
  const totalCount = response?.totalCount || 0;
  const totalPages = response?.totalPages || 1;

  const handleFilterChange = (field: string, value: any) => {
    setFilters(prev => ({ ...prev, [field]: value }));
    setPage(1); // Reset to first page when filters change
  };

  const handleSortChange = (value: string) => {
    // Map frontend sort values to backend format
    const sortMapping: { [key: string]: { sortBy: string; sortOrder: string } } = {
      'date': { sortBy: 'date', sortOrder: 'desc' },
      'price-asc': { sortBy: 'rent', sortOrder: 'asc' },
      'price-desc': { sortBy: 'rent', sortOrder: 'desc' },
    };

    const sort = sortMapping[value] || { sortBy: 'date', sortOrder: 'desc' };
    setFilters(prev => ({ ...prev, sortBy: sort.sortBy, sortOrder: sort.sortOrder }));
    setPage(1);
  };

  // Determine current sort value for the select
  const getCurrentSortValue = () => {
    if (filters.sortBy === 'date') return 'date';
    if (filters.sortBy === 'rent' && filters.sortOrder === 'asc') return 'price-asc';
    if (filters.sortBy === 'rent' && filters.sortOrder === 'desc') return 'price-desc';
    return 'date';
  };

  return (
    <Container maxWidth="lg" sx={{ py: { xs: 2, md: 4 }, px: { xs: 2, md: 3 } }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h4" component="h1" sx={{ fontSize: { xs: '1.5rem', md: '2.125rem' } }}>
          {t('apartments:title')}
        </Typography>
        {isMobile && (
          <Button
            variant="outlined"
            startIcon={<FilterListIcon />}
            onClick={() => setFilterDrawerOpen(true)}
            sx={{ minHeight: '44px' }}
          >
            {t('apartments:filters')}
          </Button>
        )}
      </Box>

      <Grid container spacing={3}>
        {/* Filters Sidebar - Desktop only */}
        <Grid item xs={12} md={3} sx={{ display: { xs: 'none', md: 'block' } }}>
          <Box sx={{ position: 'sticky', top: 100 }}>
            <Typography variant="h6" gutterBottom>
              {t('apartments:filters')}
            </Typography>

            <FormControl fullWidth margin="normal" size="small">
              <InputLabel>{t('apartments:listingType', { defaultValue: 'Listing Type' })}</InputLabel>
              <Select
                value={filters.listingType}
                onChange={(e) => handleFilterChange('listingType', e.target.value)}
                label={t('apartments:listingType', { defaultValue: 'Listing Type' })}
              >
                <MenuItem value="">{t('apartments:all', { defaultValue: 'All' })}</MenuItem>
                <MenuItem value="1">{t('apartments:forRent', { defaultValue: 'For Rent' })}</MenuItem>
                <MenuItem value="2">{t('apartments:sale', { defaultValue: 'Sale' })}</MenuItem>
              </Select>
            </FormControl>

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
                <MenuItem value="">{t('apartments:all')}</MenuItem>
                <MenuItem value="1">1</MenuItem>
                <MenuItem value="2">2</MenuItem>
                <MenuItem value="3">3</MenuItem>
                <MenuItem value="4">4+</MenuItem>
              </Select>
            </FormControl>

            <TextField
              fullWidth
              label={t('apartments:availableFrom')}
              type="date"
              value={filters.availableFrom}
              onChange={(e) => handleFilterChange('availableFrom', e.target.value)}
              margin="normal"
              size="small"
              InputLabelProps={{ shrink: true }}
            />

            <Box sx={{ mt: 2, display: 'flex', flexWrap: 'wrap', gap: 1 }}>
              {[
                { key: 'isImmediatelyAvailable', label: t('apartments:immediatelyAvailable'), type: 'boolean' },
                { key: 'isFurnished', label: t('apartments:furnished'), type: 'select' },
                { key: 'isPetFriendly', label: t('apartments:petFriendly'), type: 'select' },
                { key: 'hasParking', label: t('apartments:parking'), type: 'select' },
              ].map((filter) => {
                if (filter.type === 'boolean') {
                  return (
                    <Button
                      key={filter.key}
                      variant={filters.isImmediatelyAvailable ? "contained" : "outlined"}
                      size="small"
                      onClick={() => handleFilterChange('isImmediatelyAvailable', !filters.isImmediatelyAvailable)}
                      sx={{ borderRadius: 4, minWidth: 'auto', px: 1.5, fontSize: '0.75rem' }}
                    >
                      {filter.label}
                    </Button>
                  );
                }
                return (
                  <Button
                    key={filter.key}
                    variant={(filters as any)[filter.key] === 'true' ? "contained" : "outlined"}
                    size="small"
                    onClick={() => handleFilterChange(filter.key, (filters as any)[filter.key] === 'true' ? '' : 'true')}
                    sx={{ borderRadius: 4, minWidth: 'auto', px: 1.5, fontSize: '0.75rem' }}
                  >
                    {filter.label}
                  </Button>
                );
              })}
            </Box>
          </Box>
        </Grid>

        {/* Apartment List */}
        <Grid item xs={12} md={9}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
            <Typography variant="body1">
              {totalCount} {t('apartments:title')}
            </Typography>
            <FormControl size="small" sx={{ minWidth: 200 }}>
              <InputLabel>{t('apartments:sortBy')}</InputLabel>
              <Select
                value={getCurrentSortValue()}
                onChange={(e) => handleSortChange(e.target.value)}
                label={t('apartments:sortBy')}
              >
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
          ) : apartments.length === 0 ? (
            <Box sx={{ textAlign: 'center', py: 8 }}>
              <Typography variant="h6" color="text.secondary">
                {t('apartments:noApartments')}
              </Typography>
            </Box>
          ) : (
            <>
              <Grid container spacing={3}>
                {apartments.map((apartment) => (
                  <Grid item xs={12} sm={6} key={apartment.apartmentId}>
                    <ApartmentCard apartment={apartment} />
                  </Grid>
                ))}
              </Grid>

              {/* Pagination */}
              <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', gap: 2, mt: 4 }}>
                <Button
                  variant="outlined"
                  onClick={() => setPage(p => Math.max(1, p - 1))}
                  disabled={page === 1}
                >
                  {t('common:previous', { defaultValue: '← Previous' })}
                </Button>
                <Typography variant="body1">
                  {t('common:page', { defaultValue: 'Page' })} {page}
                </Typography>
                <Button
                  variant="outlined"
                  onClick={() => setPage(p => p + 1)}
                  disabled={page >= totalPages}
                >
                  {t('common:next', { defaultValue: 'Next →' })}
                </Button>
              </Box>
            </>
          )}
        </Grid>
      </Grid>

      {/* Mobile Filter Drawer */}
      {isMobile && (
        <Drawer
          anchor="bottom"
          open={filterDrawerOpen}
          onClose={() => setFilterDrawerOpen(false)}
          PaperProps={{
            sx: {
              maxHeight: '85vh',
              borderTopLeftRadius: 16,
              borderTopRightRadius: 16,
              p: 2,
            }
          }}
        >
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
            <Typography variant="h6">{t('apartments:filters')}</Typography>
            <IconButton onClick={() => setFilterDrawerOpen(false)} edge="end">
              <CloseIcon />
            </IconButton>
          </Box>

          <Box sx={{ overflowY: 'auto', maxHeight: 'calc(85vh - 120px)' }}>
            <FormControl fullWidth margin="normal" size="small">
              <InputLabel>{t('apartments:listingType', { defaultValue: 'Listing Type' })}</InputLabel>
              <Select
                value={filters.listingType}
                onChange={(e) => handleFilterChange('listingType', e.target.value)}
                label={t('apartments:listingType', { defaultValue: 'Listing Type' })}
              >
                <MenuItem value="">{t('apartments:all', { defaultValue: 'All' })}</MenuItem>
                <MenuItem value="1">{t('apartments:forRent', { defaultValue: 'For Rent' })}</MenuItem>
                <MenuItem value="2">{t('apartments:sale', { defaultValue: 'Sale' })}</MenuItem>
              </Select>
            </FormControl>

            <TextField
              fullWidth
              label={t('apartments:location')}
              value={filters.city}
              onChange={(e) => handleFilterChange('city', e.target.value)}
              margin="normal"
              size="small"
            />

            <Box sx={{ display: 'flex', gap: 1 }}>
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
            </Box>

            <FormControl fullWidth margin="normal" size="small">
              <InputLabel>{t('apartments:rooms')}</InputLabel>
              <Select
                value={filters.numberOfRooms}
                onChange={(e) => handleFilterChange('numberOfRooms', e.target.value)}
                label={t('apartments:rooms')}
              >
                <MenuItem value="">{t('apartments:all')}</MenuItem>
                <MenuItem value="1">1</MenuItem>
                <MenuItem value="2">2</MenuItem>
                <MenuItem value="3">3</MenuItem>
                <MenuItem value="4">4+</MenuItem>
              </Select>
            </FormControl>

            <Box sx={{ mt: 2, display: 'flex', flexWrap: 'wrap', gap: 1 }}>
              <Button
                variant={filters.isImmediatelyAvailable ? "contained" : "outlined"}
                size="small"
                onClick={() => handleFilterChange('isImmediatelyAvailable', !filters.isImmediatelyAvailable)}
                sx={{ borderRadius: 4, minHeight: '40px' }}
              >
                {t('apartments:immediatelyAvailable')}
              </Button>
              <Button
                variant={filters.isFurnished === 'true' ? "contained" : "outlined"}
                size="small"
                onClick={() => handleFilterChange('isFurnished', filters.isFurnished === 'true' ? '' : 'true')}
                sx={{ borderRadius: 4, minHeight: '40px' }}
              >
                {t('apartments:furnished')}
              </Button>
              <Button
                variant={filters.isPetFriendly === 'true' ? "contained" : "outlined"}
                size="small"
                onClick={() => handleFilterChange('isPetFriendly', filters.isPetFriendly === 'true' ? '' : 'true')}
                sx={{ borderRadius: 4, minHeight: '40px' }}
              >
                {t('apartments:petFriendly')}
              </Button>
              <Button
                variant={filters.hasParking === 'true' ? "contained" : "outlined"}
                size="small"
                onClick={() => handleFilterChange('hasParking', filters.hasParking === 'true' ? '' : 'true')}
                sx={{ borderRadius: 4, minHeight: '40px' }}
              >
                {t('apartments:parking')}
              </Button>
            </Box>
          </Box>

          <Box sx={{ mt: 2, display: 'flex', gap: 1, pt: 2, borderTop: '1px solid', borderColor: 'divider' }}>
            <Button
              fullWidth
              variant="outlined"
              onClick={() => {
                setFilters({
                  listingType: '',
                  city: '',
                  minRent: '',
                  maxRent: '',
                  numberOfRooms: '',
                  apartmentType: '',
                  isFurnished: '',
                  hasParking: '',
                  hasBalcony: '',
                  isPetFriendly: '',
                  isSmokingAllowed: '',
                  isImmediatelyAvailable: false,
                  availableFrom: '',
                  sortBy: 'date',
                  sortOrder: 'desc',
                });
                setFilterDrawerOpen(false);
              }}
            >
              {t('common:clearAll', { defaultValue: 'Clear All' })}
            </Button>
            <Button
              fullWidth
              variant="contained"
              onClick={() => setFilterDrawerOpen(false)}
            >
              {t('common:apply')}
            </Button>
          </Box>
        </Drawer>
      )}
    </Container>
  );
};

export default ApartmentListPage;
