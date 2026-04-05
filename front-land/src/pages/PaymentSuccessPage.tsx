import React, { useEffect, useState } from 'react';
import { Container, Typography, Paper, Button, Box, CircularProgress } from '@mui/material';
import { CheckCircle as CheckCircleIcon } from '@mui/icons-material';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { paymentsApi } from '../shared/api/paymentsApi';

const PaymentSuccessPage: React.FC = () => {
    const navigate = useNavigate();
    const [searchParams] = useSearchParams();
    const [refreshing, setRefreshing] = useState(false);

    useEffect(() => {
        // After redirect from Monri, refresh the JWT so the frontend
        // picks up the updated premium status set by the backend webhook.
        // The "refreshed" flag prevents re-running this on the reloaded page.
        if (searchParams.get('refreshed') === '1') return;

        const refreshToken = async () => {
            setRefreshing(true);
            try {
                const newToken = await paymentsApi.refreshToken();
                sessionStorage.setItem('authToken', newToken);
                sessionStorage.removeItem('user'); // force re-decode on next load
            } catch {
                // Webhook may not have fired yet — user can re-login later
            } finally {
                // Reload once with the flag so AuthContext re-initializes from sessionStorage
                window.location.replace('/payment-success?refreshed=1');
            }
        };

        refreshToken();
    }, [searchParams]);

    if (refreshing) {
        return (
            <Container maxWidth="sm" sx={{ py: 8, textAlign: 'center' }}>
                <CircularProgress />
                <Typography sx={{ mt: 2 }}>Aktiviramo vašu pretplatu...</Typography>
            </Container>
        );
    }

    return (
        <Container maxWidth="sm" sx={{ py: 8 }}>
            <Paper elevation={3} sx={{ p: 4, textAlign: 'center' }}>
                <CheckCircleIcon sx={{ fontSize: 80, color: 'success.main', mb: 2 }} />
                <Typography variant="h4" gutterBottom>
                    Plaćanje uspešno!
                </Typography>
                <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
                    Vaša premium pretplata je aktivna. Možete koristiti sve premium funkcije.
                </Typography>
                <Box sx={{ display: 'flex', gap: 2, justifyContent: 'center' }}>
                    <Button variant="contained" onClick={() => navigate('/profile')}>
                        Moj profil
                    </Button>
                    <Button variant="outlined" onClick={() => navigate('/analytics/roommate')}>
                        Analitika
                    </Button>
                </Box>
            </Paper>
        </Container>
    );
};

export default PaymentSuccessPage;
