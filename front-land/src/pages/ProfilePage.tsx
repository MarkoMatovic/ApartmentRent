import React from 'react';
import { Container, Typography, Box, Paper } from '@mui/material';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../shared/context/AuthContext';

const ProfilePage: React.FC = () => {
  const { t } = useTranslation('common');
  const { user } = useAuth();

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
          </Box>
        ) : (
          <Typography>Please log in to view your profile</Typography>
        )}
      </Paper>
    </Container>
  );
};

export default ProfilePage;

