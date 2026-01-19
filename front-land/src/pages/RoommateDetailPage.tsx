import React from 'react';
import {
  Container,
  Typography,
  Box,
  Grid,
  Paper,
  Button,
  Avatar,
  Card,
  CardContent,
} from '@mui/material';
import { useParams, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { roommatesApi } from '../shared/api/roommates';
import {
  Person as PersonIcon,
  LocationOn as LocationIcon,
  Euro as EuroIcon,
  CalendarToday as CalendarIcon,
} from '@mui/icons-material';

const RoommateDetailPage: React.FC = () => {
  const { t } = useTranslation(['common', 'roommates']);
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const { data: roommate, isLoading } = useQuery({
    queryKey: ['roommate', id],
    queryFn: () => {
      // Try to get by roommateId first, if that fails try by userId
      return roommatesApi.getById(Number(id)).catch(() => 
        roommatesApi.getByUserId(Number(id))
      );
    },
    enabled: !!id,
  });

  if (isLoading) {
    return (
      <Container maxWidth="lg" sx={{ py: 4 }}>
        <Typography>{t('loading')}</Typography>
      </Container>
    );
  }

  if (!roommate) {
    return (
      <Container maxWidth="lg" sx={{ py: 4 }}>
        <Typography>{t('roommates:noRoommates')}</Typography>
      </Container>
    );
  }

  const age = roommate.dateOfBirth
    ? new Date().getFullYear() - new Date(roommate.dateOfBirth).getFullYear()
    : null;

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Grid container spacing={3}>
        <Grid item xs={12} md={8}>
          {/* Profile Header */}
          <Paper sx={{ p: 3, mb: 3 }}>
            <Box sx={{ display: 'flex', gap: 3, alignItems: 'center' }}>
              <Avatar
                src={roommate.profilePicture}
                sx={{ width: 120, height: 120 }}
              >
                <PersonIcon sx={{ fontSize: 60 }} />
              </Avatar>
              <Box>
                <Typography variant="h4" component="h1" gutterBottom>
                  {roommate.firstName} {roommate.lastName}
                  {age && `, ${age}`}
                </Typography>
                {roommate.profession && (
                  <Typography variant="body1" color="text.secondary" gutterBottom>
                    {roommate.profession}
                  </Typography>
                )}
                {roommate.preferredLocation && (
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <LocationIcon color="action" />
                    <Typography variant="body2" color="text.secondary">
                      {roommate.preferredLocation}
                    </Typography>
                  </Box>
                )}
              </Box>
            </Box>
          </Paper>

          {/* About Section */}
          {roommate.bio && (
            <Paper sx={{ p: 3, mb: 3 }}>
              <Typography variant="h6" gutterBottom>
                {t('roommates:bio')}
              </Typography>
              <Typography variant="body1" paragraph>
                {roommate.bio}
              </Typography>
              {roommate.hobbies && (
                <>
                  <Typography variant="subtitle2" gutterBottom sx={{ mt: 2 }}>
                    {t('roommates:hobbies', { defaultValue: 'Hobbies' })}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {roommate.hobbies}
                  </Typography>
                </>
              )}
            </Paper>
          )}

          {/* Preferences */}
          <Paper sx={{ p: 3, mb: 3 }}>
            <Typography variant="h6" gutterBottom>
              {t('roommates:preferences')}
            </Typography>
            <Grid container spacing={2} sx={{ mt: 1 }}>
              <Grid item xs={6} sm={4}>
                <Typography variant="body2" color="text.secondary">
                  {t('roommates:smoking')}
                </Typography>
                <Typography variant="body1">
                  {roommate.smokingAllowed ? t('roommates:smokingAllowed') : 'No'}
                </Typography>
              </Grid>
              <Grid item xs={6} sm={4}>
                <Typography variant="body2" color="text.secondary">
                  {t('roommates:pets')}
                </Typography>
                <Typography variant="body1">
                  {roommate.petFriendly ? t('roommates:petFriendly') : 'No'}
                </Typography>
              </Grid>
              {roommate.lifestyle && (
                <Grid item xs={6} sm={4}>
                  <Typography variant="body2" color="text.secondary">
                    {t('roommates:lifestyle')}
                  </Typography>
                  <Typography variant="body1">
                    {t(`roommates:${roommate.lifestyle}`)}
                  </Typography>
                </Grid>
              )}
              {roommate.cleanliness && (
                <Grid item xs={6} sm={4}>
                  <Typography variant="body2" color="text.secondary">
                    {t('roommates:cleanliness')}
                  </Typography>
                  <Typography variant="body1">
                    {t(`roommates:${roommate.cleanliness}`)}
                  </Typography>
                </Grid>
              )}
              <Grid item xs={6} sm={4}>
                <Typography variant="body2" color="text.secondary">
                  {t('roommates:guests')}
                </Typography>
                <Typography variant="body1">
                  {roommate.guestsAllowed ? t('roommates:guestsAllowed') : 'No'}
                </Typography>
              </Grid>
            </Grid>
          </Paper>

          {/* Budget */}
          {(roommate.budgetMin || roommate.budgetMax) && (
            <Paper sx={{ p: 3, mb: 3 }}>
              <Typography variant="h6" gutterBottom>
                {t('roommates:budget')}
              </Typography>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
                <EuroIcon color="action" />
                <Typography variant="h5" color="primary">
                  {roommate.budgetMin && roommate.budgetMax
                    ? `€${roommate.budgetMin} - €${roommate.budgetMax}/mo`
                    : roommate.budgetMin
                    ? `€${roommate.budgetMin}+/mo`
                    : `Up to €${roommate.budgetMax}/mo`}
                </Typography>
              </Box>
              {roommate.budgetIncludes && (
                <Typography variant="body2" color="text.secondary">
                  {roommate.budgetIncludes}
                </Typography>
              )}
            </Paper>
          )}

          {/* Availability */}
          {(roommate.availableFrom || roommate.availableUntil) && (
            <Paper sx={{ p: 3, mb: 3 }}>
              <Typography variant="h6" gutterBottom>
                {t('roommates:availability', { defaultValue: 'Availability' })}
              </Typography>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <CalendarIcon color="action" />
                <Typography variant="body1">
                  {roommate.availableFrom && roommate.availableUntil
                    ? `${t('roommates:availableFrom')} ${roommate.availableFrom} - ${t('roommates:availableUntil')} ${roommate.availableUntil}`
                    : roommate.availableFrom
                    ? `${t('roommates:availableFrom')} ${roommate.availableFrom}`
                    : `${t('roommates:availableUntil')} ${roommate.availableUntil}`}
                </Typography>
              </Box>
            </Paper>
          )}

          {/* What I'm looking for */}
          {(roommate.lookingForRoomType || roommate.lookingForApartmentType || roommate.preferredLocation) && (
            <Paper sx={{ p: 3 }}>
              <Typography variant="h6" gutterBottom>
                {t('roommates:lookingFor', { defaultValue: "What I'm looking for" })}
              </Typography>
              {roommate.lookingForRoomType && (
                <Typography variant="body1" paragraph>
                  <strong>{t('roommates:roomType', { defaultValue: 'Room Type' })}:</strong>{' '}
                  {roommate.lookingForRoomType}
                </Typography>
              )}
              {roommate.lookingForApartmentType && (
                <Typography variant="body1" paragraph>
                  <strong>{t('roommates:apartmentType', { defaultValue: 'Apartment Type' })}:</strong>{' '}
                  {roommate.lookingForApartmentType}
                </Typography>
              )}
            </Paper>
          )}
        </Grid>

        <Grid item xs={12} md={4}>
          <Card sx={{ position: 'sticky', top: 100 }}>
            <CardContent>
              <Button
                fullWidth
                variant="contained"
                color="secondary"
                size="large"
                sx={{ mb: 2 }}
                onClick={() => navigate(`/messages?userId=${roommate.userId}`)}
              >
                {t('roommates:contact')}
              </Button>
              <Button
                fullWidth
                variant="outlined"
                size="large"
              >
                {t('favorites')}
              </Button>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Container>
  );
};

export default RoommateDetailPage;

