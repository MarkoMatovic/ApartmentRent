import React, { useState } from 'react';
import {
    Dialog, DialogTitle, DialogContent, DialogActions,
    Button, Typography, Alert, FormControlLabel, Checkbox, Box, Chip
} from '@mui/material';
import { Star as StarIcon } from '@mui/icons-material';
import { useTranslation } from 'react-i18next';
import { applicationsApi } from '../../shared/api/applicationsApi';
import { useNotifications } from '../../shared/context/NotificationContext';

interface ApplicationModalProps {
    open: boolean;
    onClose: () => void;
    apartmentId: number;
    apartmentTitle: string;
}

const ApplicationModal: React.FC<ApplicationModalProps> = ({ open, onClose, apartmentId, apartmentTitle }) => {
    const { t } = useTranslation('applications');
    const [loading, setLoading] = useState(false);
    const [isPriority, setIsPriority] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const { addNotification } = useNotifications();

    const handleApply = async () => {
        setLoading(true);
        setError(null);
        try {
            await applicationsApi.applyForApartment({ apartmentId, isPriority });
            addNotification({
                title: isPriority ? t('priorityApplicationSent') : t('applicationSent'),
                message: t('applicationSuccess', { title: apartmentTitle, priority: isPriority ? t('prioritySuffix') : '' }),
                type: 'success'
            });
            onClose();
            setIsPriority(false);
        } catch (err: any) {
            setError(err.response?.data || t('applicationError'));
        } finally {
            setLoading(false);
        }
    };

    const handleClose = () => {
        setIsPriority(false);
        setError(null);
        onClose();
    };

    return (
        <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
            <DialogTitle>{t('applyTitle', { title: apartmentTitle })}</DialogTitle>
            <DialogContent>
                {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
                <Typography sx={{ mb: 3 }}>
                    {t('applyConfirm')}
                </Typography>

                <Box
                    sx={{
                        border: 1,
                        borderColor: isPriority ? 'warning.main' : 'divider',
                        borderRadius: 2,
                        p: 2,
                        bgcolor: isPriority ? 'warning.50' : 'background.paper',
                        transition: 'all 0.2s',
                    }}
                >
                    <FormControlLabel
                        control={
                            <Checkbox
                                checked={isPriority}
                                onChange={(e) => setIsPriority(e.target.checked)}
                                color="warning"
                                icon={<StarIcon />}
                                checkedIcon={<StarIcon />}
                            />
                        }
                        label={
                            <Box>
                                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                    <Typography fontWeight="medium">{t('priorityApplication')}</Typography>
                                    <Chip label={t('premium')} size="small" color="warning" variant="outlined" />
                                </Box>
                                <Typography variant="body2" color="text.secondary">
                                    {t('priorityDesc')}
                                </Typography>
                            </Box>
                        }
                    />
                </Box>
            </DialogContent>
            <DialogActions>
                <Button onClick={handleClose} disabled={loading}>{t('cancel')}</Button>
                <Button
                    onClick={handleApply}
                    variant="contained"
                    color={isPriority ? 'warning' : 'primary'}
                    disabled={loading}
                    startIcon={isPriority ? <StarIcon /> : undefined}
                >
                    {loading ? t('sending') : isPriority ? t('sendPriority') : t('confirmApplication')}
                </Button>
            </DialogActions>
        </Dialog>
    );
};

export default ApplicationModal;
