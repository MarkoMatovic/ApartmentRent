import React, { useEffect, useState } from 'react';
import { Container, Typography, Paper, Button, Box, CircularProgress, Alert } from '@mui/material';
import { CheckCircle as CheckCircleIcon } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { authApi } from '../shared/api/auth';
import { useAuth } from '../shared/context/AuthContext';

const PaymentSuccessPage: React.FC = () => {
    const navigate = useNavigate();
    const { updateUser } = useAuth();
    const [refreshing, setRefreshing] = useState(true);
    const [refreshFailed, setRefreshFailed] = useState(false);

    useEffect(() => {
        const refreshSession = async () => {
            try {
                // Rotate tokens so the new JWT reflects the updated premium role
                const tokens = await authApi.rotateTokens();
                if (tokens?.accessToken) {
                    sessionStorage.setItem('authToken', tokens.accessToken);
                    // Decode and push updated user into AuthContext without a full page reload
                    const parts = tokens.accessToken.split('.');
                    if (parts.length === 3) {
                        const payload = JSON.parse(atob(parts[1].replace(/-/g, '+').replace(/_/g, '/')));
                        updateUser({
                            userId: parseInt(payload.userId || payload.nameid || payload.id) || -1,
                            userGuid: payload.sub || '',
                            firstName: payload.given_name || '',
                            lastName: payload.family_name || '',
                            email: payload.email || '',
                            isActive: true,
                            userRoleId: payload.userRoleId ? parseInt(payload.userRoleId) : undefined,
                            roleName: payload.role || payload.roleName,
                            permissions: Array.isArray(payload.permission) ? payload.permission : payload.permission ? [payload.permission] : [],
                            hasPersonalAnalytics: payload.hasPersonalAnalytics === 'true' || payload.hasPersonalAnalytics === true,
                            hasLandlordAnalytics: payload.hasLandlordAnalytics === 'true' || payload.hasLandlordAnalytics === true,
                            tokenBalance: payload.tokenBalance !== undefined ? parseInt(payload.tokenBalance) : undefined,
                            isIncognito: false,
                        });
                    }
                }
            } catch {
                // Webhook may not have fired yet — user can re-login manually
                setRefreshFailed(true);
            } finally {
                setRefreshing(false);
            }
        };

        refreshSession();
    }, []);

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
                {refreshFailed ? (
                    <Alert severity="info" sx={{ mb: 3, textAlign: 'left' }}>
                        Vaša pretplata je aktivna, ali se nismo uspeli osvežiti sesiju automatski.
                        Odjavite se i ponovo prijavite da aktivirate premium funkcije.
                    </Alert>
                ) : (
                    <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
                        Vaša premium pretplata je aktivna. Možete koristiti sve premium funkcije.
                    </Typography>
                )}
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
