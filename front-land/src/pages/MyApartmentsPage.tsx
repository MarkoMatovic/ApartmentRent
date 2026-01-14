import React from 'react';
import { Container, Typography, Box, Paper, Button, Grid, CircularProgress, Alert } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { Add as AddIcon } from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apartmentsApi } from '../shared/api/apartments';
import ApartmentCard from '../components/Apartment/ApartmentCard';

const MyApartmentsPage: React.FC = () => {
  const { t } = useTranslation(['common', 'apartments']);
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const { data: apartments, isLoading, error } = useQuery({
    queryKey: ['myApartments'],
    queryFn: () => apartmentsApi.getMyApartments(),
    retry: 1,
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, isLookingForRoommate }: { id: number; isLookingForRoommate: boolean }) =>
      apartmentsApi.update(id, { isLookingForRoommate }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['myApartments'] });
    },
  });

  const handleToggleRoommate = (apartmentId: number) => (isLookingForRoommate: boolean) => {
    updateMutation.mutate({ id: apartmentId, isLookingForRoommate });
  };

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 3 }}>
        <Typography variant="h4" component="h1">
          {t('myApartments')}
        </Typography>
        <Button
          variant="contained"
          color="secondary"
          startIcon={<AddIcon />}
          onClick={() => navigate('/apartments/create')}
        >
          {t('apartments:createApartment', { defaultValue: 'Create Apartment' })}
        </Button>
      </Box>

      {isLoading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
          <CircularProgress />
        </Box>
      )}

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {t('apartments:errorLoadingApartments', { defaultValue: 'Error loading apartments' })}
        </Alert>
      )}

      {!isLoading && !error && apartments && apartments.length > 0 && (
        <Grid container spacing={3}>
          {apartments.map((apartment) => (
            <Grid item xs={12} sm={6} md={4} key={apartment.apartmentId}>
              <ApartmentCard 
                apartment={apartment}
                isOwner={true}
                onToggleRoommate={handleToggleRoommate(apartment.apartmentId)}
                isUpdating={updateMutation.isPending}
              />
            </Grid>
          ))}
        </Grid>
      )}

      {!isLoading && !error && (!apartments || apartments.length === 0) && (
        <Paper sx={{ p: 4, textAlign: 'center' }}>
          <Typography variant="body1" color="text.secondary">
            {t('apartments:noApartments')}
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
            Your apartment listings will appear here
          </Typography>
        </Paper>
      )}
    </Container>
  );
};

export default MyApartmentsPage;

