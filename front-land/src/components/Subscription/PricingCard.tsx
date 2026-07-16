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
import { useTranslation } from 'react-i18next';

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
    const { t } = useTranslation('subscriptions');

    const features = [
        t('planAnalytics_feat5'),
        t('planAnalytics_feat2'),
        t('planAnalytics_feat3'),
        t('planAnalytics_feat1'),
        t('planAnalytics_feat4'),
        t('planAnalytics_feat6'),
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
                    label={t('currentPlan')}
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
                        {t('whatsIncluded')}
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
                    onClick={() => !isCurrentPlan && onSubscribe(plan)}
                    disabled={isCurrentPlan || loading}
                >
                    {isCurrentPlan ? t('currentPlan') : loading ? t('processing') : t('btnBuy')}
                </Button>
            </CardActions>
        </Card>
    );
};
