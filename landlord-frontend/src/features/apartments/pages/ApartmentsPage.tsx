import { useState, useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useSearchParams } from 'react-router-dom';
import {
  Box,
  Container,
  Typography,
  Grid,
  FormControl,
  Select,
  MenuItem,
  InputLabel,
  Button,
  Stack,
} from '@mui/material';
import {
  Sort as SortIcon,
  Map as MapIcon,
} from '@mui/icons-material';
import { apartmentsService } from '../api/apartmentsService';
import { ApartmentsFilters, ApartmentFilters } from '../components/apartments/ApartmentsFilters';
import { ApartmentCard } from '../components/apartments/ApartmentCard';
import { LoadingSpinner } from '@/shared/components/ui/LoadingSpinner';
import { ErrorAlert } from '@/shared/components/ui/ErrorAlert';
import { EmptyState } from '@/shared/components/ui/EmptyState';
import { AppLayout } from '@/shared/components/layout/AppLayout';

type SortOption = 'recommended' | 'price-low' | 'price-high';

const ApartmentsPage = () => {
  const theme = useTheme();
  const [searchParams, setSearchParams] = useSearchParams();
  const [sortBy, setSortBy] = useState<SortOption>('recommended');
  const [showMap, setShowMap] = useState(false);

  // Initialize filters from URL params or defaults
  const [filters, setFilters] = useState<ApartmentFilters>(() => {
    return {
      city: searchParams.get('city') || '',
      moveInDate: searchParams.get('moveInDate') || '',
      moveOutDate: searchParams.get('moveOutDate') || '',
      minPrice: searchParams.get('minPrice') ? Number(searchParams.get('minPrice')) : '',
      maxPrice: searchParams.get('maxPrice') ? Number(searchParams.get('maxPrice')) : '',
      apartmentType: (searchParams.get('type') as any) || 'all',
      bedrooms: searchParams.get('bedrooms') ? Number(searchParams.get('bedrooms')) : 'all',
      minSize: searchParams.get('minSize') ? Number(searchParams.get('minSize')) : '',
      furnished: searchParams.get('furnished') === 'true' ? true : searchParams.get('furnished') === 'false' ? false : 'all',
    };
  });

  // Fetch apartments
  const {
    data: apartments,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey: ['apartments', filters],
    queryFn: () => apartmentsService.getAllApartments(),
  });

  // Update URL params when filters change
  const handleFiltersChange = (newFilters: ApartmentFilters) => {
    setFilters(newFilters);
    const params = new URLSearchParams();
    
    if (newFilters.city) params.set('city', newFilters.city);
    if (newFilters.moveInDate) params.set('moveInDate', newFilters.moveInDate);
    if (newFilters.moveOutDate) params.set('moveOutDate', newFilters.moveOutDate);
    if (newFilters.minPrice) params.set('minPrice', String(newFilters.minPrice));
    if (newFilters.maxPrice) params.set('maxPrice', String(newFilters.maxPrice));
    if (newFilters.apartmentType !== 'all') params.set('type', newFilters.apartmentType);
    if (newFilters.bedrooms !== 'all') params.set('bedrooms', String(newFilters.bedrooms));
    if (newFilters.minSize) params.set('minSize', String(newFilters.minSize));
    if (newFilters.furnished !== 'all') params.set('furnished', String(newFilters.furnished));
    
    setSearchParams(params, { replace: true });
  };

  // Filter and sort apartments
  const filteredAndSortedApartments = useMemo(() => {
    if (!apartments) return [];

    let filtered = [...apartments];

    // Apply filters
    if (filters.city) {
      filtered = filtered.filter(
        (apt) =>
          apt.city.toLowerCase().includes(filters.city.toLowerCase()) ||
          apt.address.toLowerCase().includes(filters.city.toLowerCase())
      );
    }

    if (filters.minPrice !== '') {
      filtered = filtered.filter((apt) => apt.rent >= Number(filters.minPrice));
    }

    if (filters.maxPrice !== '') {
      filtered = filtered.filter((apt) => apt.rent <= Number(filters.maxPrice));
    }

    // Apply sorting
    switch (sortBy) {
      case 'price-low':
        filtered.sort((a, b) => a.rent - b.rent);
        break;
      case 'price-high':
        filtered.sort((a, b) => b.rent - a.rent);
        break;
      case 'recommended':
      default:
        // Keep original order (recommended/default)
        break;
    }

    return filtered;
  }, [apartments, filters, sortBy]);

  if (isLoading) {
    return (
      <AppLayout>
        <LoadingSpinner fullScreen />
      </AppLayout>
    );
  }

  if (error) {
    return (
      <AppLayout>
        <ErrorAlert
          message={error instanceof Error ? error.message : 'Failed to load apartments'}
          onRetry={() => refetch()}
        />
      </AppLayout>
    );
  }

  return (
    <AppLayout>
      <Box sx={{ bgcolor: 'background.default', minHeight: '100vh' }}>
        {/* Filters Bar */}
        <ApartmentsFilters filters={filters} onFiltersChange={handleFiltersChange} />

        <Container maxWidth="xl" sx={{ py: 4 }}>
          {/* Results Header */}
          <Box
            sx={{
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              mb: 3,
              flexWrap: 'wrap',
              gap: 2,
            }}
          >
            <Typography variant="h5" component="h1" sx={{ fontWeight: 600 }}>
              {filteredAndSortedApartments.length} {filteredAndSortedApartments.length === 1 ? 'apartment' : 'apartments'} available
            </Typography>

            <Stack direction="row" spacing={2} alignItems="center">
              {/* Sort Dropdown */}
              <FormControl size="small" sx={{ minWidth: 200 }}>
                <InputLabel>Sort by</InputLabel>
                <Select
                  value={sortBy}
                  label="Sort by"
                  onChange={(e) => setSortBy(e.target.value as SortOption)}
                  startAdornment={<SortIcon sx={{ mr: 1, fontSize: 20 }} />}
                >
                  <MenuItem value="recommended">Recommended</MenuItem>
                  <MenuItem value="price-low">Price (low to high)</MenuItem>
                  <MenuItem value="price-high">Price (high to low)</MenuItem>
                </Select>
              </FormControl>

              {/* Map Toggle Button (future-ready) */}
              <Button
                variant={showMap ? 'contained' : 'outlined'}
                startIcon={<MapIcon />}
                onClick={() => setShowMap(!showMap)}
                sx={{
                  textTransform: 'none',
                  borderRadius: 2,
                }}
              >
                Map
              </Button>
            </Stack>
          </Box>

          {/* Apartments Grid */}
          {filteredAndSortedApartments.length === 0 ? (
            <EmptyState message="No apartments found matching your filters" />
          ) : (
            <Grid container spacing={3}>
              {filteredAndSortedApartments.map((apartment) => (
                <Grid
                  item
                  xs={12}
                  sm={6}
                  md={4}
                  lg={3}
                  key={apartment.apartmentId}
                >
                  <ApartmentCard
                    apartment={apartment}
                    // Note: Backend doesn't return images in ApartmentDto
                    // These would need to be fetched separately or included in the DTO
                    // For now, we'll use placeholder images
                  />
                </Grid>
              ))}
            </Grid>
          )}
        </Container>
      </Box>
    </AppLayout>
  );
};

export default ApartmentsPage;

