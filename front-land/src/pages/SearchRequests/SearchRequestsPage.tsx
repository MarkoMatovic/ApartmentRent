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
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    TextField,
    MenuItem,
    Select,
    InputLabel,
    FormControl,
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
import { useNavigate } from 'react-router-dom';
import {
    searchRequestsApi,
    SearchRequest,
    SearchRequestInput,
    SearchRequestType,
    PagedSearchRequests,
} from '../../shared/api/searchRequests';
import { useAuth } from '../../shared/context/AuthContext';
import { format } from 'date-fns';

const emptyForm = (): SearchRequestInput => ({
    requestType: SearchRequestType.LookingForApartment,
    title: '',
    description: '',
    city: '',
    budget: undefined,
    preferredMoveInDate: '',
});

export const SearchRequestsPage: React.FC = () => {
    const { t } = useTranslation(['searchRequests']);
    const { user } = useAuth();
    const navigate = useNavigate();
    const [requests, setRequests] = useState<SearchRequest[]>([]);
    const [myRequests, setMyRequests] = useState<SearchRequest[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const [tabValue, setTabValue] = useState(0);

    // Dialog state
    const [dialogOpen, setDialogOpen] = useState(false);
    const [form, setForm] = useState<SearchRequestInput>(emptyForm());
    const [submitting, setSubmitting] = useState(false);
    const [submitError, setSubmitError] = useState('');

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
            setRequests(Array.isArray(data) ? data : (data as PagedSearchRequests).items ?? []);
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

    const handleContact = (request: SearchRequest) => {
        // Own request — nothing to do
        if (user && user.userId === request.userId) return;
        // Not logged in → send to login, come back after
        if (!user) {
            navigate('/login', { state: { from: '/search-requests' } });
            return;
        }
        // Navigate to chat with this user
        navigate(`/messages?userId=${request.userId}&name=${encodeURIComponent(request.userName ?? 'User')}`);
    };

    const openDialog = () => {
        setForm(emptyForm());
        setSubmitError('');
        setDialogOpen(true);
    };

    const handleSubmit = async () => {
        if (!form.title.trim()) { setSubmitError('Title is required.'); return; }
        if (!form.description.trim()) { setSubmitError('Description is required.'); return; }

        setSubmitting(true);
        setSubmitError('');
        try {
            const created = await searchRequestsApi.createSearchRequest({
                ...form,
                city: form.city || undefined,
                preferredMoveInDate: form.preferredMoveInDate || undefined,
            });
            setMyRequests(prev => [created, ...prev]);
            setRequests(prev => [created, ...prev]);
            setDialogOpen(false);
            setTabValue(1); // switch to My Requests so user sees it immediately
        } catch (err: any) {
            setSubmitError(err?.response?.data?.message || err.message || 'Failed to create request.');
        } finally {
            setSubmitting(false);
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
                    '&:hover': { transform: 'translateY(-4px)', boxShadow: 4 },
                }}
            >
                <CardContent sx={{ flexGrow: 1 }}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start', mb: 1 }}>
                        <Typography variant="h6" gutterBottom>
                            {request.title}
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
                                <Typography variant="body2" color="text.secondary">{request.city}</Typography>
                            </Box>
                        )}
                        {request.budget && (
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                <EuroIcon fontSize="small" color="action" />
                                <Typography variant="body2" color="text.secondary">Up to €{request.budget}</Typography>
                            </Box>
                        )}
                        {request.preferredMoveInDate && (
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                <CalendarIcon fontSize="small" color="action" />
                                <Typography variant="body2" color="text.secondary">
                                    {format(new Date(request.preferredMoveInDate), 'MMM d, yyyy')}
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
                    {user?.userId === request.userId ? (
                        <Typography variant="caption" color="text.secondary">Your request</Typography>
                    ) : (
                        <Button
                            size="small"
                            variant="contained"
                            color="primary"
                            onClick={() => handleContact(request)}
                        >
                            {user ? 'Contact' : 'Login to Contact'}
                        </Button>
                    )}
                    {showActions && (
                        <IconButton size="small" color="error" onClick={() => handleDelete(request.searchRequestId)}>
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
            {/* Header */}
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 4 }}>
                <Typography variant="h4" component="h1">{t('title')}</Typography>
                <Button variant="contained" color="primary" startIcon={<AddIcon />} size="large" onClick={openDialog}>
                    {t('createNew')}
                </Button>
            </Box>

            {error && (
                <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError('')}>{error}</Alert>
            )}

            <Paper sx={{ mb: 3 }}>
                <Tabs value={tabValue} onChange={(_, v) => setTabValue(v)}>
                    <Tab label={t('allRequests')} />
                    <Tab label={t('myRequests')} />
                </Tabs>
            </Paper>

            {tabValue === 0 && (
                requests.length === 0 ? (
                    <Paper sx={{ p: 6, textAlign: 'center' }}>
                        <SearchIcon sx={{ fontSize: 80, color: 'text.secondary', mb: 2 }} />
                        <Typography variant="h6" gutterBottom>{t('noRequests')}</Typography>
                        <Typography variant="body2" color="text.secondary">{t('noRequestsDescription')}</Typography>
                    </Paper>
                ) : (
                    <Grid container spacing={3}>
                        {requests.map(r => renderRequestCard(r, false))}
                    </Grid>
                )
            )}

            {tabValue === 1 && (
                myRequests.length === 0 ? (
                    <Paper sx={{ p: 6, textAlign: 'center' }}>
                        <SearchIcon sx={{ fontSize: 80, color: 'text.secondary', mb: 2 }} />
                        <Typography variant="h6" gutterBottom>{t('myRequestsEmpty')}</Typography>
                        <Typography variant="body2" color="text.secondary" paragraph>
                            {t('myRequestsEmptyDescription')}
                        </Typography>
                        <Button variant="contained" color="primary" startIcon={<AddIcon />} sx={{ mt: 2 }} onClick={openDialog}>
                            {t('createNew')}
                        </Button>
                    </Paper>
                ) : (
                    <Grid container spacing={3}>
                        {myRequests.map(r => renderRequestCard(r, true))}
                    </Grid>
                )
            )}

            {/* Create Dialog */}
            <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} maxWidth="sm" fullWidth>
                <DialogTitle>New Search Request</DialogTitle>
                <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: '16px !important' }}>
                    {submitError && <Alert severity="error">{submitError}</Alert>}

                    <FormControl fullWidth>
                        <InputLabel>Type</InputLabel>
                        <Select
                            value={form.requestType}
                            label="Type"
                            onChange={e => setForm(f => ({ ...f, requestType: Number(e.target.value) as SearchRequestType }))}
                        >
                            <MenuItem value={SearchRequestType.LookingForApartment}>Looking for Apartment</MenuItem>
                            <MenuItem value={SearchRequestType.LookingForRoommate}>Looking for Roommate</MenuItem>
                        </Select>
                    </FormControl>

                    <TextField
                        label="Title"
                        required
                        fullWidth
                        value={form.title}
                        onChange={e => setForm(f => ({ ...f, title: e.target.value }))}
                    />

                    <TextField
                        label="Description"
                        required
                        fullWidth
                        multiline
                        rows={3}
                        value={form.description}
                        onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
                    />

                    <TextField
                        label="City"
                        fullWidth
                        value={form.city}
                        onChange={e => setForm(f => ({ ...f, city: e.target.value }))}
                    />

                    <TextField
                        label="Budget (€)"
                        type="number"
                        fullWidth
                        value={form.budget ?? ''}
                        onChange={e => setForm(f => ({ ...f, budget: e.target.value ? Number(e.target.value) : undefined }))}
                        inputProps={{ min: 0 }}
                    />

                    <TextField
                        label="Preferred Move-in Date"
                        type="date"
                        fullWidth
                        value={form.preferredMoveInDate ?? ''}
                        onChange={e => setForm(f => ({ ...f, preferredMoveInDate: e.target.value }))}
                        InputLabelProps={{ shrink: true }}
                    />
                </DialogContent>
                <DialogActions sx={{ px: 3, pb: 2 }}>
                    <Button onClick={() => setDialogOpen(false)} disabled={submitting}>Cancel</Button>
                    <Button variant="contained" onClick={handleSubmit} disabled={submitting}>
                        {submitting ? 'Saving...' : 'Create Request'}
                    </Button>
                </DialogActions>
            </Dialog>
        </Container>
    );
};
