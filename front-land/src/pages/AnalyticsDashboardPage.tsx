import React, { useState } from 'react';
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
} from '@mui/material';
import {
    TrendingUp as TrendingUpIcon,
    Visibility as VisibilityIcon,
    Search as SearchIcon,
    Home as HomeIcon,
    People as PeopleIcon,
} from '@mui/icons-material';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { analyticsApi } from '../shared/api/analytics';
import { Link } from '@mui/material';

const AnalyticsDashboardPage: React.FC = () => {
    const { t } = useTranslation(['common', 'dashboard']);
    const navigate = useNavigate();
    const [dateRange] = useState({
        from: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
        to: new Date().toISOString().split('T')[0],
    });

    const { data: summary, isLoading: summaryLoading, error: summaryError } = useQuery({
        queryKey: ['analytics-summary', dateRange.from, dateRange.to],
        queryFn: async () => {
            console.log('📊 Fetching analytics summary...', { from: dateRange.from, to: dateRange.to });
            const result = await analyticsApi.getSummary(dateRange.from, dateRange.to);
            console.log('📊 Summary result:', result);
            return result;
        },
    });

    const { data: topApartments, isLoading: apartmentsLoading } = useQuery({
        queryKey: ['top-apartments', dateRange.from, dateRange.to],
        queryFn: async () => {
            console.log('🏠 Fetching top apartments...');
            const result = await analyticsApi.getTopApartments(10, dateRange.from, dateRange.to);
            console.log('🏠 Top apartments result:', result);
            return result;
        },
    });

    const { data: topRoommates, isLoading: roommatesLoading } = useQuery({
        queryKey: ['top-roommates', dateRange.from, dateRange.to],
        queryFn: () => analyticsApi.getTopRoommates(10, dateRange.from, dateRange.to),
    });

    const { data: topSearches, isLoading: searchesLoading } = useQuery({
        queryKey: ['top-searches', dateRange.from, dateRange.to],
        queryFn: () => analyticsApi.getTopSearches(10, dateRange.from, dateRange.to),
    });

    if (summaryLoading) {
        return (
            <Container sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '60vh' }}>
                <CircularProgress />
            </Container>
        );
    }

    if (summaryError) {
        console.error('❌ Summary error:', summaryError);
        return (
            <Container maxWidth="xl" sx={{ mt: 4 }}>
                <Alert severity="error">
                    Error loading analytics: {(summaryError as any).message}
                </Alert>
            </Container>
        );
    }

    return (
        <Container maxWidth="xl" sx={{ mt: 4, mb: 4 }}>
            <Typography variant="h4" gutterBottom>
                {t('dashboard:analyticsDashboard')}
            </Typography>
            <Typography variant="body2" color="text.secondary" gutterBottom>
                {t('dashboard:last30Days')}
            </Typography>

            {/* Summary Cards */}
            <Grid container spacing={3} sx={{ mt: 2 }}>
                <Grid item xs={12} sm={6} md={3}>
                    <Card>
                        <CardContent>
                            <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                                <VisibilityIcon color="primary" sx={{ mr: 1 }} />
                                <Typography variant="h6">{t('dashboard:totalEvents')}</Typography>
                            </Box>
                            <Typography variant="h4">{summary?.totalEvents || 0}</Typography>
                        </CardContent>
                    </Card>
                </Grid>

                <Grid item xs={12} sm={6} md={3}>
                    <Card>
                        <CardContent>
                            <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                                <HomeIcon color="success" sx={{ mr: 1 }} />
                                <Typography variant="h6">{t('dashboard:apartmentViews')}</Typography>
                            </Box>
                            <Typography variant="h4">{summary?.totalApartmentViews || 0}</Typography>
                        </CardContent>
                    </Card>
                </Grid>

                <Grid item xs={12} sm={6} md={3}>
                    <Card>
                        <CardContent>
                            <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                                <PeopleIcon color="info" sx={{ mr: 1 }} />
                                <Typography variant="h6">{t('dashboard:roommateViews')}</Typography>
                            </Box>
                            <Typography variant="h4">{summary?.totalRoommateViews || 0}</Typography>
                        </CardContent>
                    </Card>
                </Grid>

                <Grid item xs={12} sm={6} md={3}>
                    <Card>
                        <CardContent>
                            <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                                <SearchIcon color="warning" sx={{ mr: 1 }} />
                                <Typography variant="h6">{t('dashboard:searches')}</Typography>
                            </Box>
                            <Typography variant="h4">{summary?.totalSearches || 0}</Typography>
                        </CardContent>
                    </Card>
                </Grid>
            </Grid>

            {/* Top Apartments */}
            <Paper sx={{ mt: 4, p: 3 }}>
                <Typography variant="h5" gutterBottom>
                    <TrendingUpIcon sx={{ verticalAlign: 'middle', mr: 1 }} />
                    {t('dashboard:topViewedApartments')}
                </Typography>
                {apartmentsLoading ? (
                    <CircularProgress />
                ) : topApartments && topApartments.length > 0 ? (
                    <TableContainer>
                        <Table>
                            <TableHead>
                                <TableRow>
                                    <TableCell>{t('dashboard:rank')}</TableCell>
                                    <TableCell>{t('dashboard:apartmentTitle', { defaultValue: 'Apartment' })}</TableCell>
                                    <TableCell>{t('dashboard:details', { defaultValue: 'Details' })}</TableCell>
                                    <TableCell align="right">{t('dashboard:views')}</TableCell>
                                </TableRow>
                            </TableHead>
                            <TableBody>
                                {topApartments.map((apt, index) => (
                                    <TableRow key={apt.entityId}>
                                        <TableCell>
                                            <Chip label={`#${index + 1}`} color={index < 3 ? 'primary' : 'default'} size="small" />
                                        </TableCell>
                                        <TableCell>
                                            {apt.entityTitle ? (
                                                <Link
                                                    component="button"
                                                    variant="body2"
                                                    onClick={() => navigate(`/apartments/${apt.entityId}`)}
                                                    sx={{ textDecoration: 'none', cursor: 'pointer' }}
                                                >
                                                    {apt.entityTitle}
                                                </Link>
                                            ) : (
                                                `ID: ${apt.entityId}`
                                            )}
                                        </TableCell>
                                        <TableCell>
                                            {apt.entityDetails || '-'}
                                        </TableCell>
                                        <TableCell align="right">{apt.viewCount}</TableCell>
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    </TableContainer>
                ) : (
                    <Alert severity="info">{t('dashboard:noApartmentViews')}</Alert>
                )}
            </Paper>

            {/* Top Roommates */}
            <Paper sx={{ mt: 4, p: 3 }}>
                <Typography variant="h5" gutterBottom>
                    <TrendingUpIcon sx={{ verticalAlign: 'middle', mr: 1 }} />
                    {t('dashboard:topViewedRoommates')}
                </Typography>
                {roommatesLoading ? (
                    <CircularProgress />
                ) : topRoommates && topRoommates.length > 0 ? (
                    <TableContainer>
                        <Table>
                            <TableHead>
                                <TableRow>
                                    <TableCell>{t('dashboard:rank')}</TableCell>
                                    <TableCell>{t('dashboard:roommateId')}</TableCell>
                                    <TableCell align="right">{t('dashboard:views')}</TableCell>
                                </TableRow>
                            </TableHead>
                            <TableBody>
                                {topRoommates.map((rm, index) => (
                                    <TableRow key={rm.entityId}>
                                        <TableCell>
                                            <Chip label={`#${index + 1}`} color={index < 3 ? 'primary' : 'default'} size="small" />
                                        </TableCell>
                                        <TableCell>{rm.entityId}</TableCell>
                                        <TableCell align="right">{rm.viewCount}</TableCell>
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    </TableContainer>
                ) : (
                    <Alert severity="info">{t('dashboard:noRoommateViews')}</Alert>
                )}
            </Paper>

            {/* Top Searches */}
            <Paper sx={{ mt: 4, p: 3 }}>
                <Typography variant="h5" gutterBottom>
                    <SearchIcon sx={{ verticalAlign: 'middle', mr: 1 }} />
                    {t('dashboard:popularSearchTerms')}
                </Typography>
                {searchesLoading ? (
                    <CircularProgress />
                ) : topSearches && topSearches.length > 0 ? (
                    <TableContainer>
                        <Table>
                            <TableHead>
                                <TableRow>
                                    <TableCell>{t('dashboard:searchTerm')}</TableCell>
                                    <TableCell align="right">{t('dashboard:count')}</TableCell>
                                </TableRow>
                            </TableHead>
                            <TableBody>
                                {topSearches.map((search) => (
                                    <TableRow key={search.searchTerm}>
                                        <TableCell>
                                            <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                                                {search.searchTerm}
                                            </Typography>
                                        </TableCell>
                                        <TableCell align="right">{search.searchCount}</TableCell>
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    </TableContainer>
                ) : (
                    <Alert severity="info">{t('dashboard:noSearches')}</Alert>
                )}
            </Paper>
        </Container>
    );
};

export default AnalyticsDashboardPage;
