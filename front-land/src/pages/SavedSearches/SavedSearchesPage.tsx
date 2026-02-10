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
} from '@mui/material';
import {
    Delete as DeleteIcon,
    Add as AddIcon,
    Search as SearchIcon,
    LocationOn as LocationIcon,
    Bed as BedIcon,
    Euro as EuroIcon,
} from '@mui/icons-material';
import { useTranslation } from 'react-i18next';
import { savedSearchesApi, SavedSearch } from '../../shared/api/savedSearches';
import { useAuth } from '../../shared/context/AuthContext';

export const SavedSearchesPage: React.FC = () => {
    const { t } = useTranslation(['savedSearches']);
    const { user } = useAuth();
    const [searches, setSearches] = useState<SavedSearch[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');

    useEffect(() => {
        if (user?.userId) {
            loadSearches();
        }
    }, [user]);

    const loadSearches = async () => {
        try {
            setLoading(true);
            setError('');
            const data = await savedSearchesApi.getUserSavedSearches(user!.userId);
            setSearches(data);
        } catch (err: any) {
            console.error('Error loading saved searches:', err);
            setError(err.message || 'Failed to load saved searches');
        } finally {
            setLoading(false);
        }
    };

    const handleDelete = async (id: number) => {
        if (window.confirm(t('confirmDelete'))) {
            try {
                await savedSearchesApi.deleteSavedSearch(id);
                setSearches(searches.filter(s => s.savedSearchId !== id));
            } catch (err) {
                console.error('Error deleting search:', err);
                setError('Failed to delete search');
            }
        }
    };

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

            {searches.length === 0 ? (
                <Paper sx={{ p: 6, textAlign: 'center' }}>
                    <SearchIcon sx={{ fontSize: 80, color: 'text.secondary', mb: 2 }} />
                    <Typography variant="h6" gutterBottom>
                        {t('noSearches')}
                    </Typography>
                    <Typography variant="body2" color="text.secondary" paragraph>
                        {t('noSearchesDescription')}
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
                    {searches.map(search => (
                        <Grid item xs={12} sm={6} md={4} key={search.savedSearchId}>
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
                                    <Typography variant="h6" gutterBottom>
                                        {search.searchName}
                                    </Typography>

                                    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1, mt: 2 }}>
                                        {search.city && (
                                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                                <LocationIcon fontSize="small" color="action" />
                                                <Typography variant="body2" color="text.secondary">
                                                    {search.city}
                                                </Typography>
                                            </Box>
                                        )}

                                        {search.numberOfRooms && (
                                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                                <BedIcon fontSize="small" color="action" />
                                                <Typography variant="body2" color="text.secondary">
                                                    {search.numberOfRooms} rooms
                                                </Typography>
                                            </Box>
                                        )}

                                        {search.minRent && search.maxRent && (
                                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                                <EuroIcon fontSize="small" color="action" />
                                                <Typography variant="body2" color="text.secondary">
                                                    €{search.minRent} - €{search.maxRent}
                                                </Typography>
                                            </Box>
                                        )}
                                    </Box>

                                    <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap', mt: 2 }}>
                                        {search.isFurnished && (
                                            <Chip label="Furnished" size="small" />
                                        )}
                                        {search.hasParking && (
                                            <Chip label="Parking" size="small" />
                                        )}
                                        {search.hasBalcony && (
                                            <Chip label="Balcony" size="small" />
                                        )}
                                        {search.isPetFriendly && (
                                            <Chip label="Pet Friendly" size="small" color="success" />
                                        )}
                                    </Box>
                                </CardContent>

                                <CardActions sx={{ justifyContent: 'space-between', px: 2, pb: 2 }}>
                                    <Button size="small" color="primary">
                                        Apply Search
                                    </Button>
                                    <IconButton
                                        size="small"
                                        color="error"
                                        onClick={() => handleDelete(search.savedSearchId)}
                                    >
                                        <DeleteIcon />
                                    </IconButton>
                                </CardActions>
                            </Card>
                        </Grid>
                    ))}
                </Grid>
            )}
        </Container>
    );
};
