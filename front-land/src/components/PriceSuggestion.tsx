import React, { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import {
    Box,
    Button,
    Card,
    CardContent,
    Typography,
    CircularProgress,
    Alert,
    Chip,
    Divider,
} from '@mui/material';
import { AutoAwesome as AutoAwesomeIcon, TrendingUp as TrendingUpIcon } from '@mui/icons-material';
import { mlApi } from '../shared/api/analytics';
import { PricePredictionRequest } from '../shared/types/analytics';

interface PriceSuggestionProps {
    apartmentData: PricePredictionRequest;
    onPriceSelected?: (price: number) => void;
}

const PriceSuggestion: React.FC<PriceSuggestionProps> = ({ apartmentData, onPriceSelected }) => {
    const [prediction, setPrediction] = useState<{ price: number; confidence: number } | null>(null);

    const predictMutation = useMutation({
        mutationFn: (data: PricePredictionRequest) => mlApi.predictPrice(data),
        onSuccess: (data) => {
            setPrediction({
                price: data.predictedPrice,
                confidence: data.confidenceScore,
            });
        },
    });

    const handleGetSuggestion = () => {
        predictMutation.mutate(apartmentData);
    };

    const handleUseSuggestion = () => {
        if (prediction && onPriceSelected) {
            onPriceSelected(prediction.price);
        }
    };

    const getConfidenceColor = (confidence: number) => {
        if (confidence >= 80) return 'success';
        if (confidence >= 60) return 'info';
        if (confidence >= 40) return 'warning';
        return 'error';
    };

    const getConfidenceLabel = (confidence: number) => {
        if (confidence >= 80) return 'High Confidence';
        if (confidence >= 60) return 'Medium Confidence';
        if (confidence >= 40) return 'Low Confidence';
        return 'Very Low Confidence';
    };

    return (
        <Card sx={{ mt: 2, border: '2px dashed', borderColor: 'primary.main' }}>
            <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                    <AutoAwesomeIcon color="primary" sx={{ mr: 1 }} />
                    <Typography variant="h6">AI Price Suggestion</Typography>
                </Box>

                {!prediction && !predictMutation.isPending && (
                    <Box>
                        <Typography variant="body2" color="text.secondary" gutterBottom>
                            Get an AI-powered price suggestion based on your apartment's features
                        </Typography>
                        <Button
                            variant="contained"
                            startIcon={<TrendingUpIcon />}
                            onClick={handleGetSuggestion}
                            sx={{ mt: 2 }}
                            disabled={!apartmentData.sizeSquareMeters || !apartmentData.numberOfRooms}
                        >
                            Get Price Suggestion
                        </Button>
                        {(!apartmentData.sizeSquareMeters || !apartmentData.numberOfRooms) && (
                            <Typography variant="caption" color="error" display="block" sx={{ mt: 1 }}>
                                Please fill in size and number of rooms first
                            </Typography>
                        )}
                    </Box>
                )}

                {predictMutation.isPending && (
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                        <CircularProgress size={24} />
                        <Typography>Calculating optimal price...</Typography>
                    </Box>
                )}

                {predictMutation.isError && (
                    <Alert severity="error">
                        Failed to get price suggestion. {(predictMutation.error as any)?.response?.data?.message || 'Please try again.'}
                    </Alert>
                )}

                {prediction && (
                    <Box>
                        <Divider sx={{ my: 2 }} />
                        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
                            <Box>
                                <Typography variant="body2" color="text.secondary">
                                    Suggested Price
                                </Typography>
                                <Typography variant="h4" color="primary.main">
                                    €{prediction.price.toFixed(2)}
                                </Typography>
                            </Box>
                            <Chip
                                label={getConfidenceLabel(prediction.confidence)}
                                color={getConfidenceColor(prediction.confidence)}
                                icon={<TrendingUpIcon />}
                            />
                        </Box>

                        <Typography variant="caption" color="text.secondary" display="block" gutterBottom>
                            Confidence: {prediction.confidence.toFixed(1)}%
                        </Typography>

                        <Box sx={{ display: 'flex', gap: 2, mt: 2 }}>
                            <Button variant="contained" onClick={handleUseSuggestion} disabled={!onPriceSelected}>
                                Use This Price
                            </Button>
                            <Button variant="outlined" onClick={handleGetSuggestion}>
                                Recalculate
                            </Button>
                        </Box>

                        <Alert severity="info" sx={{ mt: 2 }}>
                            This suggestion is based on similar apartments in the database. You can adjust it based on your specific needs.
                        </Alert>
                    </Box>
                )}
            </CardContent>
        </Card>
    );
};

export default PriceSuggestion;
