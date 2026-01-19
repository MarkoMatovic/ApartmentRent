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
import { analyticsApi } from '../shared/api/analytics';

const AnalyticsDashboardPage: React.FC = () => {
    const [dateRange] = useState({
        from: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
        to: new Date().toISOString().split('T')[0],
    });

    const { data: summary, isLoading: summaryLoading } = useQuery({
        queryKey: ['analytics-summary', dateRange.from, dateRange.to],
        queryFn: () => analyticsApi.getSummary(dateRange.from, dateRange.to),
    });

    const { data: topApartments, isLoading: apartmentsLoading } = useQuery({
        queryKey: ['top-apartments', dateRange.from, dateRange.to],
        queryFn: () => analyticsApi.getTopApartments(10, dateRange.from, dateRange.to),
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

    return (
        <Container maxWidth="xl" sx={{ mt: 4, mb: 4 }}>
            <Typography variant="h4" gutterBottom>
                Analytics Dashboard
            </Typography>
            <Typography variant="body2" color="text.secondary" gutterBottom>
                Last 30 days
            </Typography>

            {/* Summary Cards */}
            <Grid container spacing={3} sx={{ mt: 2 }}>
                <Grid item xs={12} sm={6} md={3}>
                    <Card>
                        <CardContent>
                            <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                                <VisibilityIcon color="primary" sx={{ mr: 1 }} />
                                <Typography variant="h6">Total Events</Typography>
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
                                <Typography variant="h6">Apartment Views</Typography>
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
                                <Typography variant="h6">Roommate Views</Typography>
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
                                <Typography variant="h6">Searches</Typography>
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
                    Top Viewed Apartments
                </Typography>
                {apartmentsLoading ? (
                    <CircularProgress />
                ) : topApartments && topApartments.length > 0 ? (
                    <TableContainer>
                        <Table>
                            <TableHead>
                                <TableRow>
                                    <TableCell>Rank</TableCell>
                                    <TableCell>Apartment ID</TableCell>
                                    <TableCell align="right">Views</TableCell>
                                </TableRow>
                            </TableHead>
                            <TableBody>
                                {topApartments.map((apt, index) => (
                                    <TableRow key={apt.entityId}>
                                        <TableCell>
                                            <Chip label={`#${index + 1}`} color={index < 3 ? 'primary' : 'default'} size="small" />
                                        </TableCell>
                                        <TableCell>{apt.entityId}</TableCell>
                                        <TableCell align="right">{apt.viewCount}</TableCell>
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    </TableContainer>
                ) : (
                    <Alert severity="info">No apartment views yet</Alert>
                )}
            </Paper>

            {/* Top Roommates */}
            <Paper sx={{ mt: 4, p: 3 }}>
                <Typography variant="h5" gutterBottom>
                    <TrendingUpIcon sx={{ verticalAlign: 'middle', mr: 1 }} />
                    Top Viewed Roommates
                </Typography>
                {roommatesLoading ? (
                    <CircularProgress />
                ) : topRoommates && topRoommates.length > 0 ? (
                    <TableContainer>
                        <Table>
                            <TableHead>
                                <TableRow>
                                    <TableCell>Rank</TableCell>
                                    <TableCell>Roommate ID</TableCell>
                                    <TableCell align="right">Views</TableCell>
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
                    <Alert severity="info">No roommate views yet</Alert>
                )}
            </Paper>

            {/* Top Searches */}
            <Paper sx={{ mt: 4, p: 3 }}>
                <Typography variant="h5" gutterBottom>
                    <SearchIcon sx={{ verticalAlign: 'middle', mr: 1 }} />
                    Popular Search Terms
                </Typography>
                {searchesLoading ? (
                    <CircularProgress />
                ) : topSearches && topSearches.length > 0 ? (
                    <TableContainer>
                        <Table>
                            <TableHead>
                                <TableRow>
                                    <TableCell>Search Term</TableCell>
                                    <TableCell align="right">Count</TableCell>
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
                    <Alert severity="info">No searches yet</Alert>
                )}
            </Paper>
        </Container>
    );
};

export default AnalyticsDashboardPage;
