import React from 'react';
import { Container, Typography, Box, Paper } from '@mui/material';
import { useTranslation } from 'react-i18next';

const ChatPage: React.FC = () => {
  const { t } = useTranslation('chat');

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Typography variant="h4" component="h1" gutterBottom>
        {t('title')}
      </Typography>
      <Paper sx={{ p: 4, textAlign: 'center' }}>
        <Typography variant="body1" color="text.secondary">
          {t('noConversations')}
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
          Chat functionality will be implemented with SignalR
        </Typography>
      </Paper>
    </Container>
  );
};

export default ChatPage;

