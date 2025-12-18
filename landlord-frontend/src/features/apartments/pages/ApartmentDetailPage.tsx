import { useQuery } from '@tanstack/react-query';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Box,
  Container,
  Grid,
  Button,
} from '@mui/material';
import { apartmentsService } from '../api/apartmentsService';
import { ImageGallery } from '../components/apartment/ImageGallery';
import { ApartmentInfo } from '../components/apartment/ApartmentInfo';
import { ContactSidebar } from '../components/apartment/ContactSidebar';
import { ApartmentMap } from '../components/apartment/ApartmentMap';
import { LoadingSpinner } from '@/shared/components/ui/LoadingSpinner';
import { ErrorAlert } from '@/shared/components/ui/ErrorAlert';
import { AppLayout } from '@/shared/components/layout/AppLayout';

const ApartmentDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const apartmentId = id ? parseInt(id, 10) : 0;

  const {
    data: apartment,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey: ['apartment', apartmentId],
    queryFn: () => apartmentsService.getApartment(apartmentId),
    enabled: !!apartmentId && !isNaN(apartmentId),
  });

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
          message={error instanceof Error ? error.message : 'Failed to load apartment details'}
          onRetry={() => refetch()}
        />
      </AppLayout>
    );
  }

  if (!apartment) {
    return (
      <AppLayout>
        <Container maxWidth="lg" sx={{ py: 4 }}>
          <Box sx={{ textAlign: 'center', py: 8 }}>
            <h2>Apartment not found</h2>
            <Button
              variant="contained"
              onClick={() => navigate('/apartments')}
              sx={{ mt: 2, textTransform: 'none' }}
            >
              Back to Listings
            </Button>
          </Box>
        </Container>
      </AppLayout>
    );
  }

  // Note: Backend doesn't return images in GetApartmentDto yet
  // This would need to be added to the backend API
  // For now, we'll use an empty array and the ImageGallery will show a placeholder
  const images: string[] = [];

  const handleRequestRental = (moveInDate: string, moveOutDate: string) => {
    navigate(`/apartments/${apartmentId}/applications?moveIn=${moveInDate}&moveOut=${moveOutDate}`);
  };

  return (
    <AppLayout>
      <Box sx={{ bgcolor: 'background.default', minHeight: '100vh', py: 4 }}>
        <Container maxWidth="xl">
          {/* Image Gallery - Full Width */}
          <Box sx={{ mb: 4 }}>
            <ImageGallery
              images={images}
              apartmentId={apartment.apartmentId}
              apartmentTitle={apartment.title}
            />
          </Box>

          {/* Main Content Grid */}
          <Grid container spacing={4}>
            {/* Left Column - Main Content (~65%) */}
            <Grid item xs={12} md={8}>
              {/* Apartment Info */}
              <ApartmentInfo apartment={apartment} />

              {/* Map Section */}
              <ApartmentMap apartment={apartment} />

              {/* Back Button */}
              <Box sx={{ mt: 4 }}>
                <Button
                  variant="outlined"
                  onClick={() => navigate('/apartments')}
                  sx={{ textTransform: 'none' }}
                >
                  ← Back to Listings
                </Button>
              </Box>
            </Grid>

            {/* Right Column - Sidebar (~35%) */}
            <Grid item xs={12} md={4}>
              <ContactSidebar
                apartment={apartment}
                onRequestRental={handleRequestRental}
              />
            </Grid>
          </Grid>
        </Container>
      </Box>
    </AppLayout>
  );
};

export default ApartmentDetailPage;
