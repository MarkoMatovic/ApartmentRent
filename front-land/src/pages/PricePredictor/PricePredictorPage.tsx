import React, { useState, useEffect } from 'react';
import {
    Container,
    Typography,
    Box,
    Paper,
    Grid,
    TextField,
    Button,
    FormControlLabel,
    Checkbox,
    Card,
    CardContent,
    Alert,
    CircularProgress,
    Divider,
} from '@mui/material';
import {
    TrendingUp as TrendingUpIcon,
    Calculate as CalculateIcon,
    CheckCircle as CheckCircleIcon,
    Error as ErrorIcon,
} from '@mui/icons-material';
import { useTranslation } from 'react-i18next';
import { machineLearningApi, PricePredictionRequest, PricePredictionResponse } from '../../shared/api/machineLearning';

export const PricePredictorPage: React.FC = () => {
    const { t } = useTranslation(['machineLearning']);
    const [formData, setFormData] = useState<PricePredictionRequest>({
        city: '',
        numberOfRooms: 2,
        sizeSquareMeters: 60,
        apartmentType: 0,
        isFurnished: false,
        hasBalcony: false,
        hasParking: false,
        hasElevator: false,
    });
    const [prediction, setPrediction] = useState<PricePredictionResponse | null>(null);
    const [loading, setLoading] = useState(false);
    const [modelTrained, setModelTrained] = useState(false);
    const [checkingModel, setCheckingModel] = useState(true);

    useEffect(() => {
        checkModelStatus();
    }, []);

    const checkModelStatus = async () => {
        try {
            setCheckingModel(true);
            const isTrained = await machineLearningApi.isModelTrained();
            setModelTrained(isTrained);
        } catch (error) {
            console.error('Error checking model status:', error);
        } finally {
            setCheckingModel(false);
        }
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!modelTrained) {
            return;
        }

        try {
            setLoading(true);
            const result = await machineLearningApi.predictPrice(formData);
            setPrediction(result);
        } catch (error) {
            console.error('Error predicting price:', error);
        } finally {
            setLoading(false);
        }
    };

    const handleChange = (field: keyof PricePredictionRequest, value: any) => {
        setFormData(prev => ({ ...prev, [field]: value }));
        setPrediction(null); // Clear previous prediction
    };

    if (checkingModel) {
        return (
            <Container maxWidth="lg" sx={{ py: 4, display: 'flex', justifyContent: 'center' }}>
                <CircularProgress />
            </Container>
        );
    }

    return (
        <Container maxWidth="lg" sx={{ py: 4 }}>
            <Box sx={{ textAlign: 'center', mb: 4 }}>
                <TrendingUpIcon sx={{ fontSize: 60, color: 'primary.main', mb: 2 }} />
                <Typography variant="h4" component="h1" gutterBottom>
                    {t('pricePredictor.title')}
                </Typography>
                <Typography variant="body1" color="text.secondary">
                    {t('pricePredictor.subtitle')}
                </Typography>
            </Box>

            {!modelTrained && (
                <Alert
                    severity="warning"
                    icon={<ErrorIcon />}
                    sx={{ mb: 3 }}
                >
                    {t('pricePredictor.modelNotTrained')}
                </Alert>
            )}

            <Grid container spacing={3}>
                <Grid item xs={12} md={7}>
                    <Paper sx={{ p: 3 }}>
                        <Typography variant="h6" gutterBottom>
                            Apartment Details
                        </Typography>
                        <Divider sx={{ mb: 3 }} />

                        <form onSubmit={handleSubmit}>
                            <Grid container spacing={2}>
                                <Grid item xs={12}>
                                    <TextField
                                        fullWidth
                                        label={t('pricePredictor.city')}
                                        value={formData.city}
                                        onChange={(e) => handleChange('city', e.target.value)}
                                        required
                                        disabled={!modelTrained}
                                    />
                                </Grid>

                                <Grid item xs={12} sm={6}>
                                    <TextField
                                        fullWidth
                                        type="number"
                                        label={t('pricePredictor.rooms')}
                                        value={formData.numberOfRooms}
                                        onChange={(e) => handleChange('numberOfRooms', parseInt(e.target.value))}
                                        inputProps={{ min: 1, max: 10 }}
                                        required
                                        disabled={!modelTrained}
                                    />
                                </Grid>

                                <Grid item xs={12} sm={6}>
                                    <TextField
                                        fullWidth
                                        type="number"
                                        label={t('pricePredictor.size')}
                                        value={formData.sizeSquareMeters}
                                        onChange={(e) => handleChange('sizeSquareMeters', parseInt(e.target.value))}
                                        inputProps={{ min: 20, max: 500 }}
                                        required
                                        disabled={!modelTrained}
                                    />
                                </Grid>

                                <Grid item xs={12}>
                                    <Typography variant="subtitle2" gutterBottom sx={{ mt: 1 }}>
                                        Features
                                    </Typography>
                                    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                                        <FormControlLabel
                                            control={
                                                <Checkbox
                                                    checked={formData.isFurnished}
                                                    onChange={(e) => handleChange('isFurnished', e.target.checked)}
                                                    disabled={!modelTrained}
                                                />
                                            }
                                            label={t('pricePredictor.furnished')}
                                        />
                                        <FormControlLabel
                                            control={
                                                <Checkbox
                                                    checked={formData.hasBalcony}
                                                    onChange={(e) => handleChange('hasBalcony', e.target.checked)}
                                                    disabled={!modelTrained}
                                                />
                                            }
                                            label={t('pricePredictor.balcony')}
                                        />
                                        <FormControlLabel
                                            control={
                                                <Checkbox
                                                    checked={formData.hasParking}
                                                    onChange={(e) => handleChange('hasParking', e.target.checked)}
                                                    disabled={!modelTrained}
                                                />
                                            }
                                            label={t('pricePredictor.parking')}
                                        />
                                        <FormControlLabel
                                            control={
                                                <Checkbox
                                                    checked={formData.hasElevator}
                                                    onChange={(e) => handleChange('hasElevator', e.target.checked)}
                                                    disabled={!modelTrained}
                                                />
                                            }
                                            label={t('pricePredictor.elevator')}
                                        />
                                    </Box>
                                </Grid>

                                <Grid item xs={12}>
                                    <Button
                                        type="submit"
                                        variant="contained"
                                        color="primary"
                                        size="large"
                                        fullWidth
                                        disabled={loading || !modelTrained}
                                        startIcon={loading ? <CircularProgress size={20} /> : <CalculateIcon />}
                                        sx={{ mt: 2 }}
                                    >
                                        {loading ? 'Calculating...' : t('pricePredictor.predict')}
                                    </Button>
                                </Grid>
                            </Grid>
                        </form>
                    </Paper>
                </Grid>

                <Grid item xs={12} md={5}>
                    {prediction ? (
                        <Card
                            sx={{
                                bgcolor: 'primary.main',
                                color: 'primary.contrastText',
                                position: 'sticky',
                                top: 100,
                            }}
                        >
                            <CardContent sx={{ textAlign: 'center', py: 4 }}>
                                <CheckCircleIcon sx={{ fontSize: 60, mb: 2 }} />
                                <Typography variant="h6" gutterBottom>
                                    {t('pricePredictor.predictedPrice')}
                                </Typography>
                                <Typography variant="h2" sx={{ fontWeight: 'bold', my: 3 }}>
                                    €{prediction.predictedPrice.toLocaleString()}
                                </Typography>
                                <Typography variant="body2" sx={{ opacity: 0.9 }}>
                                    per month
                                </Typography>
                                {prediction.confidence && (
                                    <Box sx={{ mt: 3, pt: 3, borderTop: 1, borderColor: 'primary.light' }}>
                                        <Typography variant="body2" sx={{ opacity: 0.9 }}>
                                            {t('pricePredictor.confidence')}
                                        </Typography>
                                        <Typography variant="h5" sx={{ mt: 1 }}>
                                            {(prediction.confidence * 100).toFixed(1)}%
                                        </Typography>
                                    </Box>
                                )}
                            </CardContent>
                        </Card>
                    ) : (
                        <Paper
                            sx={{
                                p: 4,
                                textAlign: 'center',
                                bgcolor: 'background.default',
                                position: 'sticky',
                                top: 100,
                            }}
                        >
                            <CalculateIcon sx={{ fontSize: 80, color: 'text.disabled', mb: 2 }} />
                            <Typography variant="h6" color="text.secondary" gutterBottom>
                                Ready to Predict
                            </Typography>
                            <Typography variant="body2" color="text.secondary">
                                Fill in the apartment details and click predict to see the estimated price
                            </Typography>
                        </Paper>
                    )}
                </Grid>
            </Grid>
        </Container>
    );
};
