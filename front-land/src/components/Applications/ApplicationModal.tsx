import React, { useState } from 'react';
import {
    Dialog, DialogTitle, DialogContent, DialogActions,
    Button, Typography, Alert, FormControlLabel, Checkbox, Box, Paper, Chip
} from '@mui/material';
import { Star as StarIcon } from '@mui/icons-material';
import { applicationsApi } from '../../shared/api/applicationsApi';
import { useNotifications } from '../../shared/context/NotificationContext';

interface ApplicationModalProps {
    open: boolean;
    onClose: () => void;
    apartmentId: number;
    apartmentTitle: string;
}

const ApplicationModal: React.FC<ApplicationModalProps> = ({ open, onClose, apartmentId, apartmentTitle }) => {
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
                title: isPriority ? 'Priority Application Sent' : 'Application Sent',
                message: `Successfully applied for ${apartmentTitle}${isPriority ? ' (Priority)' : ''}`,
                type: 'success'
            });
            onClose();
            setIsPriority(false);
        } catch (err: any) {
            setError(err.response?.data || 'Failed to apply. Please try again.');
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
            <DialogTitle>Apply for {apartmentTitle}</DialogTitle>
            <DialogContent>
                {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
                <Typography sx={{ mb: 3 }}>
                    Are you sure you want to apply for this apartment? The landlord will be notified immediately.
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
                                    <Typography fontWeight="medium">Priority Application</Typography>
                                    <Chip label="Premium" size="small" color="warning" variant="outlined" />
                                </Box>
                                <Typography variant="body2" color="text.secondary">
                                    Your application appears at the top of the landlord's list.
                                </Typography>
                            </Box>
                        }
                    />
                </Box>
            </DialogContent>
            <DialogActions>
                <Button onClick={handleClose} disabled={loading}>Cancel</Button>
                <Button
                    onClick={handleApply}
                    variant="contained"
                    color={isPriority ? 'warning' : 'primary'}
                    disabled={loading}
                    startIcon={isPriority ? <StarIcon /> : undefined}
                >
                    {loading ? 'Sending...' : isPriority ? 'Send Priority Application' : 'Confirm Application'}
                </Button>
            </DialogActions>
        </Dialog>
    );
};

export default ApplicationModal;
