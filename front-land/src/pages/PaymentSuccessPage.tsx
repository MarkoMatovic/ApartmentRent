import React, { useEffect } from 'react';
import { Container, Typography, Paper, Button, Box } from '@mui/material';
import { CheckCircle as CheckCircleIcon } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../shared/context/AuthContext';
import { authApi } from '../shared/api/auth';

const PaymentSuccessPage: React.FC = () => {
    const navigate = useNavigate();
    const { token, updateUser } = useAuth();

    useEffect(() => {
        const refreshProfile = async () => {
            if (token) {
                try {
                    const profile = await authApi.getProfile();
                    updateUser(profile);
                } catch (error) {
                    console.error('Failed to refresh profile after payment', error);
                }
            }
        };
        refreshProfile();
    }, [token, updateUser]);

    return (
        <Container maxWidth="sm" sx={{ py: 8 }}>
            <Paper elevation={3} sx={{ p: 4, textAlign: 'center' }}>
                <CheckCircleIcon sx={{ fontSize: 80, color: 'success.main', mb: 2 }} />
                <Typography variant="h4" gutterBottom>
                    Payment Successful!
                </Typography>
                <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
                    Your premium subscription is now active. You can access all premium features.
                </Typography>
                <Box sx={{ display: 'flex', gap: 2, justifyContent: 'center' }}>
                    <Button variant="contained" onClick={() => navigate('/profile')}>
                        Go to Profile
                    </Button>
                    <Button variant="outlined" onClick={() => navigate('/analytics/roommate')}>
                        View Analytics
                    </Button>
                </Box>
            </Paper>
        </Container>
    );
};

export default PaymentSuccessPage;
