import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import {
  Grid,
  Card,
  CardContent,
  CardActions,
  Typography,
  Button,
  Container,
} from '@mui/material';
import { apartmentsService } from '../api/apartmentsService';
import { LoadingSpinner } from '@/shared/components/ui/LoadingSpinner';
import { ErrorAlert } from '@/shared/components/ui/ErrorAlert';
import { EmptyState } from '@/shared/components/ui/EmptyState';
import { AppLayout } from '@/shared/components/layout/AppLayout';

const ApartmentsListPage = () => {
  const navigate = useNavigate();

  const {
    data: apartments,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey: ['apartments'],
    queryFn: () => apartmentsService.getAllApartments(),
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
          message={error instanceof Error ? error.message : 'Failed to load apartments'}
          onRetry={() => refetch()}
        />
      </AppLayout>
    );
  }

  if (!apartments || apartments.length === 0) {
    return (
      <AppLayout>
        <EmptyState message="No apartments available" />
      </AppLayout>
    );
  }

  return (
    <AppLayout>
      <Container maxWidth="lg" sx={{ py: 4 }}>
        <Typography variant="h4" component="h1" gutterBottom sx={{ fontWeight: 'bold' }}>
          Available Apartments
        </Typography>
        <Grid container spacing={3} sx={{ mt: 2 }}>
          {apartments.map((apartment) => (
            <Grid item xs={12} sm={6} md={4} key={apartment.apartmentId}>
              <Card elevation={2}>
                <CardContent>
                  <Typography variant="h5" component="h2" gutterBottom>
                    {apartment.title}
                  </Typography>
                  <Typography variant="body2" color="text.secondary" gutterBottom>
                    {apartment.address}, {apartment.city}
                  </Typography>
                  <Typography variant="h6" color="primary" sx={{ mt: 2, fontWeight: 'bold' }}>
                    ${apartment.rent.toFixed(2)}/month
                  </Typography>
                </CardContent>
                <CardActions>
                  <Button
                    size="small"
                    variant="contained"
                    onClick={() => navigate(`/apartments/${apartment.apartmentId}`)}
                    sx={{ textTransform: 'none' }}
                  >
                    View Details
                  </Button>
                </CardActions>
              </Card>
            </Grid>
          ))}
        </Grid>
      </Container>
    </AppLayout>
  );
};

export default ApartmentsListPage;

