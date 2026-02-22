import React, { useEffect } from 'react';
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
import { analyticsApi } from '../shared/api/analytics';
import { applicationsApi } from '../shared/api/applicationsApi';
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
  Phone as PhoneIcon,
  Email as EmailIcon,
  Chat as ChatIcon,
  Person as PersonIcon,
  CalendarToday as CalendarIcon,
} from '@mui/icons-material';
import ApplicationModal from '../components/Applications/ApplicationModal';
import AppointmentModal from '../components/Appointments/AppointmentModal';
import LandlordProfileModal from '../components/Modals/LandlordProfileModal';
import AccountCircleIcon from '@mui/icons-material/AccountCircle';

const ApartmentDetailPage: React.FC = () => {
  const { t } = useTranslation(['common', 'apartments']);
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [openApplyModal, setOpenApplyModal] = React.useState(false);
  const [openAppointmentModal, setOpenAppointmentModal] = React.useState(false);
  const [openProfileModal, setOpenProfileModal] = React.useState(false);

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

  // Check if user has approved application for this apartment
  const { data: approvalStatus } = useQuery({
    queryKey: ['application-approval', id],
    queryFn: () => applicationsApi.checkApprovalStatus(Number(id)),
    enabled: !!id,
    retry: false,
  });

  // Track apartment view when component mounts and apartment data is loaded
  useEffect(() => {
    if (apartment && id) {
      analyticsApi.trackEvent(
        'ApartmentView',
        'Listings',
        Number(id),
        'Apartment'
      );
    }
  }, [apartment, id]);

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

      {/* Landlord Info */}
      {apartment.landlordName && (
        <Box display="flex" alignItems="center" gap={1} mb={2}>
          <PersonIcon fontSize="small" color="action" />
          <Typography variant="body2" color="text.secondary">
            Listed by: {apartment.landlordName}
          </Typography>
          {apartment.landlordId && (
            <Button
              size="small"
              startIcon={<AccountCircleIcon />}
              onClick={() => setOpenProfileModal(true)}
              sx={{ ml: 1 }}
            >
              View Profile
            </Button>
          )}
        </Box>
      )}

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
                <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                  {t('apartments:utilitiesIncluded')}
                </Typography>
              )}

              <Divider sx={{ my: 2 }} />

              {/* Landlord Info */}
              {apartment.landlordName && (
                <Box sx={{ mb: 2 }}>
                  <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                    {t('apartments:landlord', { defaultValue: 'Landlord' })}
                  </Typography>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <PersonIcon color="action" />
                    <Typography variant="body1">{apartment.landlordName}</Typography>
                  </Box>
                </Box>
              )}

              {/* Contact Options */}
              <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                {t('apartments:contactOptions', { defaultValue: 'Contact Options' })}
              </Typography>

              {/* Phone - display only */}
              {apartment.contactPhone && (
                <Box sx={{ mt: 1, p: 1.5, border: 1, borderColor: 'divider', borderRadius: 1, display: 'flex', alignItems: 'center', gap: 1 }}>
                  <PhoneIcon color="action" />
                  <Typography variant="body1" sx={{ fontWeight: 500 }}>
                    {apartment.contactPhone}
                  </Typography>
                </Box>
              )}

              {/* Email */}
              {apartment.landlordEmail && (
                <Button
                  fullWidth
                  variant="outlined"
                  color="primary"
                  size="large"
                  startIcon={<EmailIcon />}
                  sx={{ mt: 1 }}
                  href={`mailto:${apartment.landlordEmail}?subject=${encodeURIComponent(t('apartments:emailSubject', { defaultValue: 'Inquiry about: ' }) + apartment.title)}`}
                >
                  {t('apartments:sendEmail', { defaultValue: 'Send Email' })}
                </Button>
              )}

              {/* Chat */}
              <Button
                fullWidth
                variant="contained"
                color="secondary"
                size="large"
                startIcon={<ChatIcon />}
                sx={{ mt: 1 }}
                onClick={() => {
                  if (apartment.landlordId) {
                    // Track contact click
                    analyticsApi.trackEvent(
                      'ContactClick',
                      'Listings',
                      Number(id),
                      'Apartment'
                    );
                    navigate(`/messages?userId=${apartment.landlordId}`);
                  }
                }}
                disabled={!apartment.landlordId}
              >
                {t('apartments:sendMessage', { defaultValue: 'Send Message' })}
              </Button>

              {/* Schedule Viewing Button */}
              <Button
                fullWidth
                variant="outlined"
                color="primary"
                size="large"
                startIcon={<CalendarIcon />}
                sx={{ mt: 1 }}
                onClick={() => setOpenAppointmentModal(true)}
                disabled={!approvalStatus?.hasApprovedApplication}
              >
                {t('apartments:scheduleViewing', { defaultValue: 'Schedule Viewing' })}
              </Button>

              {/* Show message if application not approved */}
              {!approvalStatus?.hasApprovedApplication && (
                <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: 'block', textAlign: 'center' }}>
                  {approvalStatus?.applicationStatus === 'Pending'
                    ? t('apartments:applicationPending', { defaultValue: 'Your application is pending approval' })
                    : t('apartments:applyFirst', { defaultValue: 'Apply for this apartment to schedule a viewing' })
                  }
                </Typography>
              )}

              {/* Apply Button */}
              <Button
                fullWidth
                variant="contained"
                color="primary"
                size="large"
                sx={{ mt: 1 }}
                onClick={() => setOpenApplyModal(true)}
              >
                Apply for Apartment
              </Button>

              <ApplicationModal
                open={openApplyModal}
                onClose={() => setOpenApplyModal(false)}
                apartmentId={Number(id)}
                apartmentTitle={apartment.title}
              />

              <AppointmentModal
                open={openAppointmentModal}
                onClose={() => setOpenAppointmentModal(false)}
                apartmentId={Number(id)}
                apartmentTitle={apartment.title}
              />

              {/* Looking for roommate notice */}
              {apartment.isLookingForRoommate && (
                <Box sx={{ mt: 2, p: 2, bgcolor: 'success.light', borderRadius: 1 }}>
                  <Typography variant="body2" sx={{ color: 'success.contrastText', fontWeight: 600 }}>
                    {t('apartments:lookingForRoommateNotice', { defaultValue: '🏠 This apartment is looking for a roommate! Contact the landlord to learn more.' })}
                  </Typography>
                </Box>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Landlord Profile Modal */}
      {apartment.landlordId && (
        <LandlordProfileModal
          open={openProfileModal}
          onClose={() => setOpenProfileModal(false)}
          userId={apartment.landlordId}
        />
      )}
    </Container>
  );
};

export default ApartmentDetailPage;

