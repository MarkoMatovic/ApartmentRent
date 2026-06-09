import React, { useEffect, useState } from 'react';
import { Container, Typography, Paper, Button, Box, CircularProgress, Alert } from '@mui/material';
import { CheckCircle as CheckCircleIcon } from '@mui/icons-material';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { authApi } from '../shared/api/auth';
import { useAuth } from '../shared/context/AuthContext';
import { setAccessToken } from '../shared/api/tokenStore';

/** Infer a friendly description from the Monri order_number query param. */
const getSuccessMessage = (orderNumber: string | null): { title: string; body: string; primaryPath: string; primaryLabel: string } => {
    if (!orderNumber) return { title: 'Plaćanje uspešno!', body: 'Vaša usluga je aktivirana.', primaryPath: '/moje-pretplate', primaryLabel: 'Moje pretplate' };

    const parts = orderNumber.split('_');
    const planId = parts[1] ?? '';

    if (planId.startsWith('analytics'))
        return { title: 'Analitika aktivirana!', body: 'Pristup naprednoj analitici i ML predviđanju cena je sada aktivan.', primaryPath: '/analytics/roommate', primaryLabel: 'Otvori analitiku' };
    if (planId.startsWith('tokens'))
        return { title: 'Tokeni dodati!', body: 'Tokeni su dodati na vaš balans. Koristite ih za Super-Like i direktne poruke.', primaryPath: '/roommates', primaryLabel: 'Pronađi cimere' };
    if (planId.startsWith('featured'))
        return { title: 'Oglas istaknut!', body: 'Vaš oglas će se prikazivati na vrhu pretrage dok je istaknuće aktivno.', primaryPath: '/my-apartments', primaryLabel: 'Moji oglasi' };
    if (planId.startsWith('listing'))
        return { title: 'Listing krediti dodati!', body: 'Možete objaviti nove oglase koristeći kupljene kredite.', primaryPath: '/apartments/create', primaryLabel: 'Objavi oglas' };
    if (planId === 'boost-7')
        return { title: 'Boost aktiviran!', body: 'Vaš profil cimera prikazuje se prvi u rezultatima pretrage narednih 7 dana.', primaryPath: '/roommates', primaryLabel: 'Pregled profila' };
    if (planId === 'priority-30')
        return { title: 'Priority Inbox aktiviran!', body: 'Poruke od verifikovanih korisnika biće označene prioritetom narednih 30 dana.', primaryPath: '/messages', primaryLabel: 'Poruke' };

    return { title: 'Plaćanje uspešno!', body: 'Vaša usluga je aktivirana.', primaryPath: '/moje-pretplate', primaryLabel: 'Moje pretplate' };
};

const PaymentSuccessPage: React.FC = () => {
    const navigate = useNavigate();
    const [searchParams] = useSearchParams();
    const { updateUser } = useAuth();
    const [refreshing, setRefreshing] = useState(true);
    const [refreshFailed, setRefreshFailed] = useState(false);

    const orderNumber = searchParams.get('order_number');
    const { title, body, primaryPath, primaryLabel } = getSuccessMessage(orderNumber);

    useEffect(() => {
        const refreshSession = async () => {
            try {
                // Rotate tokens so JWT reflects updated role/tokenBalance
                const tokens = await authApi.rotateTokens();
                if (tokens?.accessToken) {
                    // Keep access token in memory only — never write to sessionStorage (XSS risk).
                    setAccessToken(tokens.accessToken);
                    window.dispatchEvent(new Event('authTokenChanged'));
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
                            listingCredits: payload.listingCredits !== undefined ? parseInt(payload.listingCredits) : 0,
                            isIncognito: false,
                        });
                    }
                }
            } catch {
                // Monri webhook may not have fired yet — user can refresh manually
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
                <Typography sx={{ mt: 2 }}>Aktiviramo vašu uslugu...</Typography>
            </Container>
        );
    }

    return (
        <Container maxWidth="sm" sx={{ py: 8 }}>
            <Paper elevation={3} sx={{ p: 4, textAlign: 'center' }}>
                <CheckCircleIcon sx={{ fontSize: 80, color: 'success.main', mb: 2 }} />
                <Typography variant="h4" gutterBottom>{title}</Typography>

                {refreshFailed ? (
                    <Alert severity="info" sx={{ mb: 3, textAlign: 'left' }}>
                        Usluga je aktivirana ali nismo uspeli automatski osvežiti sesiju.
                        Odjavite se i ponovo prijavite da vidite promjene.
                    </Alert>
                ) : (
                    <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
                        {body}
                    </Typography>
                )}

                {orderNumber && (
                    <Typography variant="caption" color="text.disabled" display="block" sx={{ mb: 3 }}>
                        Br. narudžbine: {orderNumber}
                    </Typography>
                )}

                <Box sx={{ display: 'flex', gap: 2, justifyContent: 'center', flexWrap: 'wrap' }}>
                    <Button variant="contained" onClick={() => navigate(primaryPath)}>
                        {primaryLabel}
                    </Button>
                    <Button variant="outlined" onClick={() => navigate('/moje-pretplate')}>
                        Moje pretplate
                    </Button>
                    <Button variant="outlined" onClick={() => navigate('/istorija-placanja')}>
                        Istorija plaćanja
                    </Button>
                </Box>
            </Paper>
        </Container>
    );
};

export default PaymentSuccessPage;
