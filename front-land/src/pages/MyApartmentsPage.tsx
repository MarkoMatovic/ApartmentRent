import React, { useState } from 'react';
import { Container, Typography, Box, Paper, Button, Grid, CircularProgress, Alert, Dialog, DialogTitle, DialogContent, DialogContentText, DialogActions } from '@mui/material';
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
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [apartmentToDelete, setApartmentToDelete] = useState<number | null>(null);

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

  const deleteMutation = useMutation({
    mutationFn: (id: number) => apartmentsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['myApartments'] });
      queryClient.invalidateQueries({ queryKey: ['apartments'] });
      setDeleteDialogOpen(false);
      setApartmentToDelete(null);
    },
  });

  const handleToggleRoommate = (apartmentId: number) => (isLookingForRoommate: boolean) => {
    updateMutation.mutate({ id: apartmentId, isLookingForRoommate });
  };

  const handleEdit = (apartmentId: number) => {
    navigate(`/apartments/edit/${apartmentId}`);
  };

  const handleDeleteClick = (apartmentId: number) => {
    setApartmentToDelete(apartmentId);
    setDeleteDialogOpen(true);
  };

  const handleDeleteConfirm = () => {
    if (apartmentToDelete) {
      deleteMutation.mutate(apartmentToDelete);
    }
  };

  const handleDeleteCancel = () => {
    setDeleteDialogOpen(false);
    setApartmentToDelete(null);
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
                onEdit={() => handleEdit(apartment.apartmentId)}
                onDelete={() => handleDeleteClick(apartment.apartmentId)}
                isUpdating={updateMutation.isPending}
                isDeleting={deleteMutation.isPending && apartmentToDelete === apartment.apartmentId}
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

      {/* Delete Confirmation Dialog */}
      <Dialog
        open={deleteDialogOpen}
        onClose={handleDeleteCancel}
      >
        <DialogTitle>
          {t('apartments:deleteApartment', { defaultValue: 'Delete Apartment' })}
        </DialogTitle>
        <DialogContent>
          <DialogContentText>
            {t('apartments:deleteApartmentConfirm', { defaultValue: 'Are you sure you want to delete this apartment listing? This action cannot be undone.' })}
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleDeleteCancel} disabled={deleteMutation.isPending}>
            {t('common:cancel', { defaultValue: 'Cancel' })}
          </Button>
          <Button 
            onClick={handleDeleteConfirm} 
            color="error" 
            variant="contained"
            disabled={deleteMutation.isPending}
          >
            {deleteMutation.isPending 
              ? t('common:deleting', { defaultValue: 'Deleting...' })
              : t('common:delete', { defaultValue: 'Delete' })}
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
};

export default MyApartmentsPage;

