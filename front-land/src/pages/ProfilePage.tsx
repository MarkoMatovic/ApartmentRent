import React, { useState } from 'react';
import { Container, Typography, Box, Paper, FormControlLabel, Switch } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../shared/context/AuthContext';
import { authApi } from '../shared/api/auth';

const ProfilePage: React.FC = () => {
  const { t } = useTranslation('common');
  const { user, updateUser } = useAuth();
  const [isLookingForRoommate, setIsLookingForRoommate] = useState(user?.isLookingForRoommate ?? false);
  const [isUpdating, setIsUpdating] = useState(false);

  const handleToggleRoommateStatus = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const newValue = event.target.checked;
    setIsLookingForRoommate(newValue);
    setIsUpdating(true);

    try {
      if (user?.userGuid) {
        await authApi.updateRoommateStatus(user.userGuid, newValue);
        // Update user in context
        if (updateUser) {
          updateUser({ ...user, isLookingForRoommate: newValue });
        }
      }
    } catch (error) {
      console.error('Failed to update roommate status:', error);
      setIsLookingForRoommate(!newValue);
    } finally {
      setIsUpdating(false);
    }
  };

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Typography variant="h4" component="h1" gutterBottom>
        {t('profile')}
      </Typography>
      <Paper sx={{ p: 4 }}>
        {user ? (
          <Box>
            <Typography variant="h6" gutterBottom>
              {user.firstName} {user.lastName}
            </Typography>
            <Typography variant="body1" color="text.secondary">
              {user.email}
            </Typography>
            {user.phoneNumber && (
              <Typography variant="body1" color="text.secondary">
                {user.phoneNumber}
              </Typography>
            )}
            <Box sx={{ mt: 3 }}>
              <FormControlLabel
                control={
                  <Switch
                    checked={isLookingForRoommate}
                    onChange={handleToggleRoommateStatus}
                    disabled={isUpdating}
                    color="primary"
                  />
                }
                label={
                  <Box>
                    <Typography variant="body1">{t('lookingForRoommate')}</Typography>
                    <Typography variant="caption" color="text.secondary">
                      {t('lookingForRoommateDescription')}
                    </Typography>
                  </Box>
                }
              />
            </Box>
          </Box>
        ) : (
          <Typography>Please log in to view your profile</Typography>
        )}
      </Paper>
    </Container>
  );
};

export default ProfilePage;

