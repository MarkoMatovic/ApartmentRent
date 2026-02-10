import React, { useEffect } from 'react';
import { Container, Typography, Paper, Button, Box } from '@mui/material';
import { CheckCircle as CheckCircleIcon } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';

const PaymentSuccessPage: React.FC = () => {
    const navigate = useNavigate();

    useEffect(() => {
        // Optionally refresh user data to reflect premium status
        // This could trigger a re-fetch of the user profile
    }, []);

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
