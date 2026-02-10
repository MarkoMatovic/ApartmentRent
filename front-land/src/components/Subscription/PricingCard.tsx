import React from 'react';
import {
    Card,
    CardContent,
    CardActions,
    Typography,
    Button,
    Box,
    Chip,
} from '@mui/material';
import { Check as CheckIcon } from '@mui/icons-material';
import { SubscriptionPlan } from '../../shared/types/subscription';

interface PricingCardProps {
    plan: SubscriptionPlan;
    onSubscribe: (plan: SubscriptionPlan) => void;
    isCurrentPlan?: boolean;
    loading?: boolean;
}

export const PricingCard: React.FC<PricingCardProps> = ({
    plan,
    onSubscribe,
    isCurrentPlan = false,
    loading = false,
}) => {
    const features = [
        'Personal analytics dashboard',
        'Apartment view tracking',
        'Message analytics',
        'Search behavior insights',
        'ML-powered price predictions (landlords)',
        'Advanced listing analytics (landlords)',
    ];

    return (
        <Card
            sx={{
                height: '100%',
                display: 'flex',
                flexDirection: 'column',
                position: 'relative',
                border: isCurrentPlan ? 2 : 1,
                borderColor: isCurrentPlan ? 'primary.main' : 'divider',
            }}
        >
            {isCurrentPlan && (
                <Chip
                    label="Current Plan"
                    color="primary"
                    size="small"
                    sx={{
                        position: 'absolute',
                        top: 16,
                        right: 16,
                    }}
                />
            )}

            <CardContent sx={{ flexGrow: 1, pt: isCurrentPlan ? 4 : 3 }}>
                <Typography variant="h5" component="h2" gutterBottom fontWeight="bold">
                    {plan.name}
                </Typography>

                <Box sx={{ mb: 3 }}>
                    <Typography variant="h3" component="div" color="primary" fontWeight="bold">
                        €{plan.price.toFixed(2)}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                        per {plan.interval}
                    </Typography>
                </Box>

                <Typography variant="body2" color="text.secondary" paragraph>
                    {plan.description}
                </Typography>

                <Box sx={{ mt: 3 }}>
                    <Typography variant="subtitle2" gutterBottom fontWeight="medium">
                        What's included:
                    </Typography>
                    {features.map((feature, index) => (
                        <Box
                            key={index}
                            sx={{
                                display: 'flex',
                                alignItems: 'center',
                                gap: 1,
                                mb: 1,
                            }}
                        >
                            <CheckIcon fontSize="small" color="success" />
                            <Typography variant="body2">{feature}</Typography>
                        </Box>
                    ))}
                </Box>
            </CardContent>

            <CardActions sx={{ p: 2, pt: 0 }}>
                <Button
                    variant={isCurrentPlan ? "outlined" : "contained"}
                    fullWidth
                    size="large"
                    onClick={() => onSubscribe(plan)}
                    disabled={isCurrentPlan || loading}
                >
                    {isCurrentPlan ? 'Active' : 'Get Premium'}
                </Button>
            </CardActions>
        </Card>
    );
};
