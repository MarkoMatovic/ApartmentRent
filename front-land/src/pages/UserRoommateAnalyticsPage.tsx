import React from 'react';
import { useQuery } from '@tanstack/react-query';
import {
    Container,
    Typography,
    Box,
    Grid,
    Paper,
    Card,
    CardContent,
    CircularProgress,
    Alert,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    Chip,
    Link as MuiLink,
} from '@mui/material';
import {
    Home as HomeIcon,
    Visibility as VisibilityIcon,
    Message as MessageIcon,
} from '@mui/icons-material';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../shared/context/AuthContext';
import { analyticsApi } from '../shared/api/analytics';
import PremiumBlur from '../components/Premium/PremiumBlur';

const PersonalAnalyticsPage: React.FC = () => {
    const { t } = useTranslation(['common', 'analytics']);
    const navigate = useNavigate();
    const { user } = useAuth();

    // Apartments I viewed
    const { data: viewedApartments, isLoading: viewedLoading, error: viewedError } = useQuery({
        queryKey: ['my-viewed-apartments'],
        queryFn: () => analyticsApi.getMyViewedApartments(10),
        enabled: !!user?.userId,
        retry: false,
    });

    // My apartments' views (if landlord)
    const { data: myApartmentViews, isLoading: landlordLoading, error: landlordError } = useQuery({
        queryKey: ['my-apartment-views'],
        queryFn: () => analyticsApi.getMyApartmentViews(),
        enabled: !!user?.userId,
        retry: false,
    });

    // Messages sent
    const { data: messagesSent, isLoading: messagesLoading, error: messagesError } = useQuery({
        queryKey: ['my-messages-sent'],
        queryFn: () => analyticsApi.getMyMessagesSent(),
        enabled: !!user?.userId,
        retry: false,
    });

    // Debug logging
    React.useEffect(() => {
        console.log('📊 PERSONAL ANALYTICS DATA:', {
            viewedApartments,
            myApartmentViews,
            messagesSent,
            viewedError,
            landlordError,
            messagesError,
        });
    }, [viewedApartments, myApartmentViews, messagesSent, viewedError, landlordError, messagesError]);

    if (!user) {
        return (
            <Container sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '60vh' }}>
                <Alert severity="warning">{t('analytics:roommateAnalytics:loginRequired')}</Alert>
            </Container>
        );
    }

    if (viewedLoading || landlordLoading || messagesLoading) {
        return (
            <Container sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '60vh' }}>
                <CircularProgress />
            </Container>
        );
    }

    const totalApartmentViewsOfMine = myApartmentViews?.reduce((sum: number, apt: any) => sum + (apt.viewCount || 0), 0) || 0;

    return (
        <Container maxWidth="xl" sx={{ mt: 4, mb: 4 }}>
            <Typography variant="h4" gutterBottom>
                {t('analytics:analytics:personalAnalytics')}
            </Typography>
            <Typography variant="body2" color="text.secondary" gutterBottom>
                {t('analytics:analytics:viewYourActivity')}
            </Typography>

            <PremiumBlur
                feature="personalAnalytics"
                requiredFeature="Personalna Analitika"
            >
                {/* Summary Cards */}
                <Grid container spacing={3} sx={{ mt: 2 }}>
                    <Grid item xs={12} sm={6} md={4}>
                        <Card>
                            <CardContent>
                                <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                                    <HomeIcon color="primary" sx={{ mr: 1 }} />
                                    <Typography variant="h6">{t('analytics:analytics:viewedApartments')}</Typography>
                                </Box>
                                <Typography variant="h4">{viewedApartments?.length || 0}</Typography>
                            </CardContent>
                        </Card>
                    </Grid>

                    <Grid item xs={12} sm={6} md={4}>
                        <Card>
                            <CardContent>
                                <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                                    <VisibilityIcon color="success" sx={{ mr: 1 }} />
                                    <Typography variant="h6">{t('analytics:analytics:myApartmentViews')}</Typography>
                                </Box>
                                <Typography variant="h4">{totalApartmentViewsOfMine}</Typography>
                            </CardContent>
                        </Card>
                    </Grid>

                    <Grid item xs={12} sm={6} md={4}>
                        <Card>
                            <CardContent>
                                <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                                    <MessageIcon color="info" sx={{ mr: 1 }} />
                                    <Typography variant="h6">{t('analytics:analytics:messagesSent')}</Typography>
                                </Box>
                                <Typography variant="h4">{messagesSent || 0}</Typography>
                            </CardContent>
                        </Card>
                    </Grid>
                </Grid>

                {/* Apartments I Viewed */}
                <Paper sx={{ mt: 4, p: 3 }}>
                    <Typography variant="h5" gutterBottom>
                        <HomeIcon sx={{ verticalAlign: 'middle', mr: 1 }} />
                        {t('analytics:analytics:apartmentsMostViewed')}
                    </Typography>
                    {viewedError ? (
                        <Alert severity="error">{t('analytics:analytics:errorLoading')}</Alert>
                    ) : viewedApartments && viewedApartments.length > 0 ? (
                        <TableContainer>
                            <Table>
                                <TableHead>
                                    <TableRow>
                                        <TableCell>{t('analytics:analytics:rank')}</TableCell>
                                        <TableCell>{t('analytics:analytics:apartment')}</TableCell>
                                        <TableCell>{t('analytics:analytics:details')}</TableCell>
                                        <TableCell align="right">{t('analytics:analytics:views')}</TableCell>
                                    </TableRow>
                                </TableHead>
                                <TableBody>
                                    {viewedApartments.map((apt: any, index: number) => (
                                        <TableRow key={apt.entityId}>
                                            <TableCell>
                                                <Chip label={`#${index + 1}`} color={index < 3 ? 'primary' : 'default'} size="small" />
                                            </TableCell>
                                            <TableCell>
                                                {apt.entityTitle ? (
                                                    <MuiLink
                                                        component="button"
                                                        variant="body2"
                                                        onClick={() => navigate(`/apartments/${apt.entityId}`)}
                                                        sx={{ textDecoration: 'none', cursor: 'pointer' }}
                                                    >
                                                        {apt.entityTitle}
                                                    </MuiLink>
                                                ) : (
                                                    `Apartman #${apt.entityId}`
                                                )}
                                            </TableCell>
                                            <TableCell>{apt.entityDetails || '-'}</TableCell>
                                            <TableCell align="right">{apt.viewCount}</TableCell>
                                        </TableRow>
                                    ))}
                                </TableBody>
                            </Table>
                        </TableContainer>
                    ) : (
                        <Alert severity="info">{t('analytics:analytics:noApartmentsViewed')}</Alert>
                    )}
                </Paper>

                {/* My Apartments Views */}
                <Paper sx={{ mt: 4, p: 3 }}>
                    <Typography variant="h5" gutterBottom>
                        <VisibilityIcon sx={{ verticalAlign: 'middle', mr: 1 }} />
                        {t('analytics:analytics:myApartmentsStats')}
                    </Typography>
                    {landlordError ? (
                        <Alert severity="error">{t('analytics:analytics:errorLoading')}</Alert>
                    ) : myApartmentViews && myApartmentViews.length > 0 ? (
                        <TableContainer>
                            <Table>
                                <TableHead>
                                    <TableRow>
                                        <TableCell>{t('analytics:analytics:apartment')}</TableCell>
                                        <TableCell>{t('analytics:analytics:city')}</TableCell>
                                        <TableCell>{t('analytics:analytics:price')}</TableCell>
                                        <TableCell align="right">{t('analytics:analytics:viewCount')}</TableCell>
                                        <TableCell>{t('analytics:analytics:lastViewed')}</TableCell>
                                    </TableRow>
                                </TableHead>
                                <TableBody>
                                    {myApartmentViews.map((apt: any) => (
                                        <TableRow key={apt.apartmentId}>
                                            <TableCell>
                                                <MuiLink
                                                    component="button"
                                                    variant="body2"
                                                    onClick={() => navigate(`/apartments/${apt.apartmentId}`)}
                                                    sx={{ textDecoration: 'none', cursor: 'pointer' }}
                                                >
                                                    {apt.title}
                                                </MuiLink>
                                            </TableCell>
                                            <TableCell>{apt.city || '-'}</TableCell>
                                            <TableCell>€{apt.rent}/mo</TableCell>
                                            <TableCell align="right">
                                                <Chip
                                                    label={apt.viewCount}
                                                    color={apt.viewCount > 10 ? 'success' : apt.viewCount > 5 ? 'primary' : 'default'}
                                                    size="small"
                                                />
                                            </TableCell>
                                            <TableCell>
                                                {apt.lastViewed ? new Date(apt.lastViewed).toLocaleDateString('sr-RS') : '-'}
                                            </TableCell>
                                        </TableRow>
                                    ))}
                                </TableBody>
                            </Table>
                        </TableContainer>
                    ) : (
                        <Alert severity="info">{t('analytics:analytics:noApartmentsOwned')}</Alert>
                    )}
                </Paper>
            </PremiumBlur>
        </Container >
    );
};

export default PersonalAnalyticsPage;
