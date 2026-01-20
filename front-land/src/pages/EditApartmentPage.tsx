import React, { useState, useEffect } from 'react';
import {
    Container,
    Paper,
    TextField,
    Button,
    Typography,
    Box,
    Grid,
    FormControlLabel,
    Checkbox,
    Alert,
    CircularProgress,
} from '@mui/material';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { apartmentsApi } from '../shared/api/apartments';
import { ApartmentUpdateInputDto } from '../shared/types/apartment';
import { FormControl, InputLabel, Select, MenuItem } from '@mui/material';

const EditApartmentPage: React.FC = () => {
    const { t } = useTranslation(['common', 'apartments']);
    const navigate = useNavigate();
    const { id } = useParams<{ id: string }>();
    const queryClient = useQueryClient();
    const [error, setError] = useState('');
    const [success, setSuccess] = useState(false);

    const { data: apartment, isLoading: isLoadingApartment } = useQuery({
        queryKey: ['apartment', id],
        queryFn: () => apartmentsApi.getById(Number(id)),
        enabled: !!id,
    });

    const [formData, setFormData] = useState<ApartmentUpdateInputDto>({
        isLookingForRoommate: false,
    });

    useEffect(() => {
        if (apartment) {
            setFormData({
                title: apartment.title,
                description: apartment.description,
                rent: apartment.rent,
                price: apartment.price,
                address: apartment.address,
                city: apartment.city,
                postalCode: apartment.postalCode,
                availableFrom: apartment.availableFrom,
                availableUntil: apartment.availableUntil,
                numberOfRooms: apartment.numberOfRooms,
                rentIncludeUtilities: apartment.rentIncludeUtilities,
                sizeSquareMeters: apartment.sizeSquareMeters,
                listingType: apartment.listingType,
                isFurnished: apartment.isFurnished,
                hasBalcony: apartment.hasBalcony,
                hasElevator: apartment.hasElevator,
                hasParking: apartment.hasParking,
                hasInternet: apartment.hasInternet,
                hasAirCondition: apartment.hasAirCondition,
                isPetFriendly: apartment.isPetFriendly,
                isSmokingAllowed: apartment.isSmokingAllowed,
                depositAmount: apartment.depositAmount,
                minimumStayMonths: apartment.minimumStayMonths,
                maximumStayMonths: apartment.maximumStayMonths,
                isImmediatelyAvailable: apartment.isImmediatelyAvailable,
                isLookingForRoommate: apartment.isLookingForRoommate || false,
                contactPhone: apartment.contactPhone || '',
            });
        }
    }, [apartment]);

    const updateMutation = useMutation({
        mutationFn: (data: ApartmentUpdateInputDto) => apartmentsApi.update(Number(id!), data),
        onSuccess: () => {
            setSuccess(true);
            queryClient.invalidateQueries({ queryKey: ['apartments'] });
            queryClient.invalidateQueries({ queryKey: ['apartment', id] });
            queryClient.invalidateQueries({ queryKey: ['myApartments'] });
            setTimeout(() => {
                navigate('/my-apartments');
            }, 2000);
        },
        onError: (err: any) => {
            let errorMessage = 'Failed to update apartment listing';

            if (err.response?.data?.message) {
                errorMessage = err.response.data.message;
            } else if (err.response?.data) {
                errorMessage = typeof err.response.data === 'string'
                    ? err.response.data
                    : JSON.stringify(err.response.data);
            } else if (err.message) {
                errorMessage = err.message;
            }

            setError(errorMessage);
        },
    });

    const handleChange = (field: keyof ApartmentUpdateInputDto, value: any) => {
        setFormData({ ...formData, [field]: value });
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError('');

        updateMutation.mutate(formData);
    };

    if (isLoadingApartment) {
        return (
            <Container maxWidth="md" sx={{ py: 8 }}>
                <Box sx={{ display: 'flex', justifyContent: 'center' }}>
                    <CircularProgress />
                </Box>
            </Container>
        );
    }

    if (!apartment) {
        return (
            <Container maxWidth="md" sx={{ py: 8 }}>
                <Alert severity="error">
                    {t('apartments:apartmentNotFound', { defaultValue: 'Apartment not found' })}
                </Alert>
            </Container>
        );
    }

    if (success) {
        return (
            <Container maxWidth="md" sx={{ py: 8 }}>
                <Paper sx={{ p: 4 }}>
                    <Alert severity="success" sx={{ mb: 2 }}>
                        {t('apartments:apartmentUpdated', { defaultValue: 'Apartment listing updated successfully!' })}
                    </Alert>
                </Paper>
            </Container>
        );
    }

    return (
        <Container maxWidth="md" sx={{ py: 4 }}>
            <Paper elevation={3} sx={{ p: 4 }}>
                <Typography variant="h4" component="h1" gutterBottom align="center">
                    {t('apartments:editApartment', { defaultValue: 'Edit Apartment Listing' })}
                </Typography>

                {error && (
                    <Alert severity="error" sx={{ mb: 2 }}>
                        {error}
                    </Alert>
                )}

                <Box component="form" onSubmit={handleSubmit} sx={{ mt: 3 }}>
                    <Grid container spacing={2}>
                        <Grid item xs={12}>
                            <TextField
                                fullWidth
                                label={t('apartments:title')}
                                value={formData.title || ''}
                                onChange={(e) => handleChange('title', e.target.value)}
                                margin="normal"
                            />
                        </Grid>

                        <Grid item xs={12}>
                            <TextField
                                fullWidth
                                label={t('apartments:description')}
                                multiline
                                rows={4}
                                value={formData.description || ''}
                                onChange={(e) => handleChange('description', e.target.value)}
                                margin="normal"
                            />
                        </Grid>

                        {/* Show Price for Sale, Rent for Rent */}
                        {formData.listingType === 2 ? (
                            <Grid item xs={12} sm={6}>
                                <TextField
                                    fullWidth
                                    label={`${t('apartments:price', { defaultValue: 'Sale Price' })} (€)`}
                                    type="number"
                                    value={formData.price || ''}
                                    onChange={(e) => handleChange('price', e.target.value ? parseFloat(e.target.value) : undefined)}
                                    margin="normal"
                                />
                            </Grid>
                        ) : (
                            <Grid item xs={12} sm={6}>
                                <TextField
                                    fullWidth
                                    label={`${t('apartments:monthlyRent', { defaultValue: 'Monthly Rent' })} (€)`}
                                    type="number"
                                    value={formData.rent || ''}
                                    onChange={(e) => handleChange('rent', parseFloat(e.target.value) || 0)}
                                    margin="normal"
                                />
                            </Grid>
                        )}

                        <Grid item xs={12} sm={6}>
                            <TextField
                                fullWidth
                                label={t('apartments:address')}
                                value={formData.address || ''}
                                onChange={(e) => handleChange('address', e.target.value)}
                                margin="normal"
                            />
                        </Grid>

                        <Grid item xs={12} sm={6}>
                            <TextField
                                fullWidth
                                label={t('apartments:city', { defaultValue: 'City' })}
                                value={formData.city || ''}
                                onChange={(e) => handleChange('city', e.target.value)}
                                margin="normal"
                            />
                        </Grid>

                        <Grid item xs={12} sm={6}>
                            <TextField
                                fullWidth
                                label={t('apartments:postalCode', { defaultValue: 'Postal Code' })}
                                value={formData.postalCode || ''}
                                onChange={(e) => handleChange('postalCode', e.target.value)}
                                margin="normal"
                            />
                        </Grid>

                        <Grid item xs={12} sm={6}>
                            <TextField
                                fullWidth
                                label={t('apartments:contactPhone', { defaultValue: 'Contact Phone' })}
                                value={formData.contactPhone || ''}
                                onChange={(e) => handleChange('contactPhone', e.target.value)}
                                margin="normal"
                                placeholder="+381 64 123 4567"
                            />
                        </Grid>

                        <Grid item xs={12} sm={6}>
                            <FormControl fullWidth margin="normal">
                                <InputLabel>{t('apartments:listingType', { defaultValue: 'Listing Type' })}</InputLabel>
                                <Select
                                    value={formData.listingType || 1}
                                    onChange={(e) => handleChange('listingType', e.target.value)}
                                    label={t('apartments:listingType', { defaultValue: 'Listing Type' })}
                                >
                                    <MenuItem value={1}>{t('apartments:forRent', { defaultValue: 'For Rent' })}</MenuItem>
                                    <MenuItem value={2}>{t('apartments:sale', { defaultValue: 'Sale' })}</MenuItem>
                                </Select>
                            </FormControl>
                        </Grid>

                        <Grid item xs={12}>
                            <FormControlLabel
                                control={
                                    <Checkbox
                                        checked={formData.isLookingForRoommate || false}
                                        onChange={(e) => handleChange('isLookingForRoommate', e.target.checked)}
                                    />
                                }
                                label={t('apartments:lookingForRoommate', { defaultValue: 'Looking for Roommate' })}
                            />
                        </Grid>

                        <Grid item xs={12}>
                            <FormControlLabel
                                control={
                                    <Checkbox
                                        checked={formData.isImmediatelyAvailable || false}
                                        onChange={(e) => handleChange('isImmediatelyAvailable', e.target.checked)}
                                    />
                                }
                                label={t('apartments:immediatelyAvailable')}
                            />
                        </Grid>

                        <Grid item xs={12}>
                            <Box sx={{ display: 'flex', gap: 2, mt: 2 }}>
                                <Button
                                    type="submit"
                                    variant="contained"
                                    color="primary"
                                    disabled={updateMutation.isPending}
                                >
                                    {updateMutation.isPending
                                        ? t('common:saving', { defaultValue: 'Saving...' })
                                        : t('common:save', { defaultValue: 'Save Changes' })}
                                </Button>
                                <Button
                                    variant="outlined"
                                    onClick={() => navigate('/my-apartments')}
                                >
                                    {t('common:cancel', { defaultValue: 'Cancel' })}
                                </Button>
                            </Box>
                        </Grid>
                    </Grid>
                </Box>
            </Paper>
        </Container>
    );
};

export default EditApartmentPage;
