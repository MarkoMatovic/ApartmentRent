import React, { useState, useEffect } from 'react';
import {
    Container,
    Typography,
    Box,
    Paper,
    Grid,
    Card,
    CardContent,
    CardActions,
    Button,
    Chip,
    IconButton,
    CircularProgress,
    Alert,
    Tabs,
    Tab,
} from '@mui/material';
import {
    Delete as DeleteIcon,
    Add as AddIcon,
    LocationOn as LocationIcon,
    Euro as EuroIcon,
    CalendarToday as CalendarIcon,
    Search as SearchIcon,
} from '@mui/icons-material';
import { useTranslation } from 'react-i18next';
import { searchRequestsApi, SearchRequest } from '../../shared/api/searchRequests';
import { useAuth } from '../../shared/context/AuthContext';
import { format } from 'date-fns';

export const SearchRequestsPage: React.FC = () => {
    const { t } = useTranslation(['searchRequests']);
    const { user } = useAuth();
    const [requests, setRequests] = useState<SearchRequest[]>([]);
    const [myRequests, setMyRequests] = useState<SearchRequest[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const [tabValue, setTabValue] = useState(0);

    useEffect(() => {
        loadRequests();
        if (user?.userId) {
            loadMyRequests();
        }
    }, [user]);

    const loadRequests = async () => {
        try {
            setLoading(true);
            setError('');
            const data = await searchRequestsApi.getAllSearchRequests();
            setRequests(data);
        } catch (err: any) {
            console.error('Error loading search requests:', err);
            setError(err.message || 'Failed to load search requests');
        } finally {
            setLoading(false);
        }
    };

    const loadMyRequests = async () => {
        try {
            const data = await searchRequestsApi.getUserSearchRequests(user!.userId);
            setMyRequests(data);
        } catch (err) {
            console.error('Error loading my requests:', err);
        }
    };

    const handleDelete = async (id: number) => {
        if (window.confirm(t('confirmDelete'))) {
            try {
                await searchRequestsApi.deleteSearchRequest(id);
                setMyRequests(myRequests.filter(r => r.searchRequestId !== id));
                setRequests(requests.filter(r => r.searchRequestId !== id));
            } catch (err) {
                console.error('Error deleting request:', err);
                setError('Failed to delete request');
            }
        }
    };

    const renderRequestCard = (request: SearchRequest, showActions: boolean = false) => (
        <Grid item xs={12} sm={6} md={4} key={request.searchRequestId}>
            <Card
                sx={{
                    height: '100%',
                    display: 'flex',
                    flexDirection: 'column',
                    transition: 'transform 0.2s, box-shadow 0.2s',
                    '&:hover': {
                        transform: 'translateY(-4px)',
                        boxShadow: 4,
                    }
                }}
            >
                <CardContent sx={{ flexGrow: 1 }}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start', mb: 1 }}>
                        <Typography variant="h6" gutterBottom>
                            {request.requestTitle}
                        </Typography>
                        <Chip
                            label={request.isActive ? t('active') : t('inactive')}
                            size="small"
                            color={request.isActive ? 'success' : 'default'}
                        />
                    </Box>

                    <Typography variant="body2" color="text.secondary" paragraph sx={{ mb: 2 }}>
                        {request.description?.substring(0, 100)}
                        {request.description && request.description.length > 100 && '...'}
                    </Typography>

                    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                        {request.city && (
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                <LocationIcon fontSize="small" color="action" />
                                <Typography variant="body2" color="text.secondary">
                                    {request.city}
                                </Typography>
                            </Box>
                        )}

                        {request.budget && (
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                <EuroIcon fontSize="small" color="action" />
                                <Typography variant="body2" color="text.secondary">
                                    Up to €{request.budget}
                                </Typography>
                            </Box>
                        )}

                        {request.moveInDate && (
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                <CalendarIcon fontSize="small" color="action" />
                                <Typography variant="body2" color="text.secondary">
                                    {format(new Date(request.moveInDate), 'MMM d, yyyy')}
                                </Typography>
                            </Box>
                        )}
                    </Box>

                    {request.userName && (
                        <Box sx={{ mt: 2, pt: 2, borderTop: 1, borderColor: 'divider' }}>
                            <Typography variant="caption" color="text.secondary">
                                Posted by: {request.userName}
                            </Typography>
                        </Box>
                    )}
                </CardContent>

                <CardActions sx={{ justifyContent: 'space-between', px: 2, pb: 2 }}>
                    <Button size="small" color="primary">
                        Contact
                    </Button>
                    {showActions && (
                        <IconButton
                            size="small"
                            color="error"
                            onClick={() => handleDelete(request.searchRequestId)}
                        >
                            <DeleteIcon />
                        </IconButton>
                    )}
                </CardActions>
            </Card>
        </Grid>
    );

    if (loading) {
        return (
            <Container maxWidth="lg" sx={{ py: 4, display: 'flex', justifyContent: 'center' }}>
                <CircularProgress />
            </Container>
        );
    }

    return (
        <Container maxWidth="lg" sx={{ py: 4 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 4 }}>
                <Typography variant="h4" component="h1">
                    {t('title')}
                </Typography>
                <Button
                    variant="contained"
                    color="primary"
                    startIcon={<AddIcon />}
                    size="large"
                >
                    {t('createNew')}
                </Button>
            </Box>

            {error && (
                <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError('')}>
                    {error}
                </Alert>
            )}

            <Paper sx={{ mb: 3 }}>
                <Tabs value={tabValue} onChange={(_, newValue) => setTabValue(newValue)}>
                    <Tab label={t('allRequests')} />
                    <Tab label={t('myRequests')} />
                </Tabs>
            </Paper>

            {tabValue === 0 && (
                <>
                    {requests.length === 0 ? (
                        <Paper sx={{ p: 6, textAlign: 'center' }}>
                            <SearchIcon sx={{ fontSize: 80, color: 'text.secondary', mb: 2 }} />
                            <Typography variant="h6" gutterBottom>
                                {t('noRequests')}
                            </Typography>
                            <Typography variant="body2" color="text.secondary">
                                {t('noRequestsDescription')}
                            </Typography>
                        </Paper>
                    ) : (
                        <Grid container spacing={3}>
                            {requests.map(request => renderRequestCard(request, false))}
                        </Grid>
                    )}
                </>
            )}

            {tabValue === 1 && (
                <>
                    {myRequests.length === 0 ? (
                        <Paper sx={{ p: 6, textAlign: 'center' }}>
                            <SearchIcon sx={{ fontSize: 80, color: 'text.secondary', mb: 2 }} />
                            <Typography variant="h6" gutterBottom>
                                {t('myRequestsEmpty')}
                            </Typography>
                            <Typography variant="body2" color="text.secondary" paragraph>
                                {t('myRequestsEmptyDescription')}
                            </Typography>
                            <Button
                                variant="contained"
                                color="primary"
                                startIcon={<AddIcon />}
                                sx={{ mt: 2 }}
                            >
                                {t('createNew')}
                            </Button>
                        </Paper>
                    ) : (
                        <Grid container spacing={3}>
                            {myRequests.map(request => renderRequestCard(request, true))}
                        </Grid>
                    )}
                </>
            )}
        </Container>
    );
};
