import React, { useEffect, useState } from 'react';
import { Container, Typography, Paper, Button, Box, CircularProgress, Alert } from '@mui/material';
import { CheckCircle as CheckCircleIcon } from '@mui/icons-material';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { authApi } from '../shared/api/auth';
import { useAuth } from '../shared/context/AuthContext';
import { setAccessToken } from '../shared/api/tokenStore';

const PaymentSuccessPage: React.FC = () => {
    const { t } = useTranslation('payments');
    const navigate = useNavigate();
    const [searchParams] = useSearchParams();
    const { updateUser } = useAuth();
    const [refreshing, setRefreshing] = useState(true);
    const [refreshFailed, setRefreshFailed] = useState(false);

    const orderNumber = searchParams.get('order_number');

    const getSuccessContent = (): { title: string; body: string; primaryPath: string; primaryLabel: string } => {
        if (!orderNumber) return { title: t('successDefault_title'), body: t('successDefault_body'), primaryPath: '/moje-pretplate', primaryLabel: t('btnSubscriptions') };

        const planId = orderNumber.split('_')[1] ?? '';

        if (planId.startsWith('analytics'))
            return { title: t('successAnalytics_title'), body: t('successAnalytics_body'), primaryPath: '/analytics/roommate', primaryLabel: t('successAnalytics_btn') };
        if (planId.startsWith('tokens'))
            return { title: t('successTokens_title'), body: t('successTokens_body'), primaryPath: '/roommates', primaryLabel: t('successTokens_btn') };
        if (planId.startsWith('featured'))
            return { title: t('successFeatured_title'), body: t('successFeatured_body'), primaryPath: '/my-apartments', primaryLabel: t('successFeatured_btn') };
        if (planId.startsWith('listing'))
            return { title: t('successListing_title'), body: t('successListing_body'), primaryPath: '/apartments/create', primaryLabel: t('successListing_btn') };
        if (planId === 'boost-7')
            return { title: t('successBoost_title'), body: t('successBoost_body'), primaryPath: '/roommates', primaryLabel: t('successBoost_btn') };
        if (planId === 'priority-30')
            return { title: t('successPriority_title'), body: t('successPriority_body'), primaryPath: '/messages', primaryLabel: t('successPriority_btn') };

        return { title: t('successDefault_title'), body: t('successDefault_body'), primaryPath: '/moje-pretplate', primaryLabel: t('btnSubscriptions') };
    };

    useEffect(() => {
        const refreshSession = async () => {
            try {
                const tokens = await authApi.rotateTokens();
                if (tokens?.accessToken) {
                    setAccessToken(tokens.accessToken);
                    window.dispatchEvent(new Event('authTokenChanged'));
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
                <Typography sx={{ mt: 2 }}>{t('activating')}</Typography>
            </Container>
        );
    }

    const { title, body, primaryPath, primaryLabel } = getSuccessContent();

    return (
        <Container maxWidth="sm" sx={{ py: 8 }}>
            <Paper elevation={3} sx={{ p: 4, textAlign: 'center' }}>
                <CheckCircleIcon sx={{ fontSize: 80, color: 'success.main', mb: 2 }} />
                <Typography variant="h4" gutterBottom>{title}</Typography>

                {refreshFailed ? (
                    <Alert severity="info" sx={{ mb: 3, textAlign: 'left' }}>
                        {t('sessionRefreshFailed')}
                    </Alert>
                ) : (
                    <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
                        {body}
                    </Typography>
                )}

                {orderNumber && (
                    <Typography variant="caption" color="text.disabled" display="block" sx={{ mb: 3 }}>
                        {t('orderRef')}: {orderNumber}
                    </Typography>
                )}

                <Box sx={{ display: 'flex', gap: 2, justifyContent: 'center', flexWrap: 'wrap' }}>
                    <Button variant="contained" onClick={() => navigate(primaryPath)}>
                        {primaryLabel}
                    </Button>
                    <Button variant="outlined" onClick={() => navigate('/moje-pretplate')}>
                        {t('btnSubscriptions')}
                    </Button>
                    <Button variant="outlined" onClick={() => navigate('/istorija-placanja')}>
                        {t('btnPaymentHistory')}
                    </Button>
                </Box>
            </Paper>
        </Container>
    );
};

export default PaymentSuccessPage;
