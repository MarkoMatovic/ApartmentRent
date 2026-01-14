import React from 'react';
import {
  Container,
  Typography,
  Box,
  Grid,
  Paper,
  Chip,
  Button,
  Divider,
  Card,
  CardContent,
} from '@mui/material';
import { useParams, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { apartmentsApi } from '../shared/api/apartments';
import { roommatesApi } from '../shared/api/roommates';
import ApartmentMap from '../components/Map/ApartmentMap';
import RoommateCard from '../components/Roommate/RoommateCard';
import ApartmentImageGallery from '../components/Apartment/ApartmentImageGallery';
import ReviewsSection from '../components/Review/ReviewsSection';
import StarRating from '../components/Review/StarRating';
import {
  LocationOn as LocationIcon,
  Euro as EuroIcon,
  Bed as BedIcon,
  SquareFoot as SquareFootIcon,
} from '@mui/icons-material';

const ApartmentDetailPage: React.FC = () => {
  const { t } = useTranslation(['common', 'apartments']);
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const { data: apartment, isLoading } = useQuery({
    queryKey: ['apartment', id],
    queryFn: () => apartmentsApi.getById(Number(id)),
    enabled: !!id,
  });

  const { data: roommatesLookingForThisApartment } = useQuery({
    queryKey: ['roommates', 'apartment', id],
    queryFn: () => roommatesApi.getAll({ apartmentId: Number(id) } as any),
    enabled: !!id,
  });

  if (isLoading) {
    return (
      <Container maxWidth="lg" sx={{ py: 4 }}>
        <Typography>Loading...</Typography>
      </Container>
    );
  }

  if (!apartment) {
    return (
      <Container maxWidth="lg" sx={{ py: 4 }}>
        <Typography>Apartment not found</Typography>
      </Container>
    );
  }

  const hasLocation = apartment.latitude && apartment.longitude;

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Typography variant="h4" component="h1" gutterBottom>
        {apartment.title}
      </Typography>

      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, flexWrap: 'wrap', mb: 2 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <LocationIcon color="action" />
          <Typography variant="h6" color="text.secondary">
            {apartment.address}, {apartment.city}
          </Typography>
        </Box>

        {apartment.averageRating !== undefined && apartment.reviewCount !== undefined && apartment.reviewCount > 0 && (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <StarRating rating={apartment.averageRating} size="medium" showNumber />
            <Typography variant="body2" color="text.secondary">
              ({apartment.reviewCount} {t('reviews:reviews', { defaultValue: 'reviews' })})
            </Typography>
          </Box>
        )}
      </Box>

      <Typography variant="h5" color="primary" sx={{ mb: 3 }}>
        <EuroIcon sx={{ verticalAlign: 'middle', mr: 1 }} />
        {apartment.rent}/mo
      </Typography>

      {/* Image Gallery */}
      <Box sx={{ mb: 4 }}>
        <ApartmentImageGallery
          images={apartment.apartmentImages || []}
          title={apartment.title}
          isLookingForRoommate={apartment.isLookingForRoommate === true}
        />
      </Box>

      <Grid container spacing={3}>
        <Grid item xs={12} md={8}>
          {/* Description */}
          <Paper sx={{ p: 3, mb: 3 }}>
            <Typography variant="h6" gutterBottom>
              {t('apartments:description')}
            </Typography>
            <Typography variant="body1" paragraph>
              {apartment.description || 'No description available.'}
            </Typography>
          </Paper>

          {/* Map */}
          {hasLocation && (
            <Paper sx={{ p: 3, mb: 3 }}>
              <Typography variant="h6" gutterBottom>
                {t('apartments:location')}
              </Typography>
              <ApartmentMap
                latitude={apartment.latitude!}
                longitude={apartment.longitude!}
                address={apartment.address}
              />
            </Paper>
          )}

          {/* Roommates Looking for Room in This Apartment */}
          {roommatesLookingForThisApartment && roommatesLookingForThisApartment.length > 0 && (
            <Paper sx={{ p: 3, mb: 3 }}>
              <Typography variant="h6" gutterBottom>
                {t('roommates:lookingForRoomInThisApartment', { defaultValue: 'Roommates Looking for Room in This Apartment' })}
              </Typography>
              <Grid container spacing={2} sx={{ mt: 1 }}>
                {roommatesLookingForThisApartment.slice(0, 3).map((roommate) => (
                  <Grid item xs={12} sm={6} md={4} key={roommate.userId}>
                    <RoommateCard roommate={roommate} />
                  </Grid>
                ))}
              </Grid>
              {roommatesLookingForThisApartment.length > 3 && (
                <Box sx={{ mt: 2, textAlign: 'center' }}>
                  <Button
                    variant="outlined"
                    onClick={() => navigate(`/roommates?apartmentId=${id}`)}
                  >
                    {t('roommates:viewAll', { defaultValue: 'View All' })} ({roommatesLookingForThisApartment.length})
                  </Button>
                </Box>
              )}
            </Paper>
          )}

          {/* Details */}
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              {t('apartments:details')}
            </Typography>
            <Grid container spacing={2}>
              <Grid item xs={6} sm={4}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <BedIcon color="action" />
                  <Typography variant="body2">
                    {apartment.numberOfRooms} {t('apartments:rooms')}
                  </Typography>
                </Box>
              </Grid>
              {apartment.sizeSquareMeters && (
                <Grid item xs={6} sm={4}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <SquareFootIcon color="action" />
                    <Typography variant="body2">
                      {apartment.sizeSquareMeters} m²
                    </Typography>
                  </Box>
                </Grid>
              )}
            </Grid>

            <Divider sx={{ my: 2 }} />

            <Typography variant="subtitle2" gutterBottom>
              {t('apartments:features')}
            </Typography>
            <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', mt: 1 }}>
              {apartment.isFurnished && (
                <Chip label={t('apartments:furnished')} size="small" />
              )}
              {apartment.hasParking && (
                <Chip label={t('apartments:parking')} size="small" />
              )}
              {apartment.hasBalcony && (
                <Chip label={t('apartments:balcony')} size="small" />
              )}
              {apartment.hasElevator && (
                <Chip label={t('apartments:elevator')} size="small" />
              )}
              {apartment.hasInternet && (
                <Chip label={t('apartments:internet')} size="small" />
              )}
              {apartment.hasAirCondition && (
                <Chip label={t('apartments:airCondition')} size="small" />
              )}
              {apartment.isPetFriendly && (
                <Chip label={t('apartments:petFriendly')} size="small" color="success" />
              )}
              {apartment.isSmokingAllowed && (
                <Chip label={t('apartments:smokingAllowed')} size="small" />
              )}
            </Box>
          </Paper>

          {/* Reviews Section */}
          <Paper sx={{ p: 3, mt: 3 }}>
            <ReviewsSection apartmentId={Number(id)} />
          </Paper>
        </Grid>

        <Grid item xs={12} md={4}>
          <Card sx={{ position: 'sticky', top: 100 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                {t('apartments:rent')}
              </Typography>
              <Typography variant="h4" color="primary" gutterBottom>
                €{apartment.rent}/mo
              </Typography>
              {apartment.rentIncludeUtilities && (
                <Typography variant="body2" color="text.secondary">
                  {t('apartments:utilitiesIncluded')}
                </Typography>
              )}
              <Button
                fullWidth
                variant="contained"
                color="secondary"
                size="large"
                sx={{ mt: 3 }}
                onClick={() => {
                  if (apartment.landlordId) {
                    navigate(`/messages?userId=${apartment.landlordId}`);
                  }
                }}
                disabled={!apartment.landlordId}
              >
                {t('contact')}
              </Button>
              <Button
                fullWidth
                variant="outlined"
                size="large"
                sx={{ mt: 1 }}
              >
                {t('apply')}
              </Button>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Container>
  );
};

export default ApartmentDetailPage;

