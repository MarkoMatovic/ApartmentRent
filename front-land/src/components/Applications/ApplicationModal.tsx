import React, { useState } from 'react';
import { Dialog, DialogTitle, DialogContent, DialogActions, Button, Typography, Alert } from '@mui/material';
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
    const [error, setError] = useState<string | null>(null);
    const { addNotification } = useNotifications();

    const handleApply = async () => {
        setLoading(true);
        setError(null);
        try {
            await applicationsApi.applyForApartment({ apartmentId });
            addNotification({
                title: 'Application Sent',
                message: `Successfully applied for ${apartmentTitle}`,
                type: 'success'
            });
            onClose();
        } catch (err: any) {
            setError(err.response?.data || 'Failed to apply. Please try again.');
        } finally {
            setLoading(false);
        }
    };

    return (
        <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
            <DialogTitle>Apply for {apartmentTitle}</DialogTitle>
            <DialogContent>
                {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
                <Typography>
                    Are you sure you want to apply for this apartment? The landlord will be notified immediately.
                </Typography>
            </DialogContent>
            <DialogActions>
                <Button onClick={onClose} disabled={loading}>Cancel</Button>
                <Button onClick={handleApply} variant="contained" disabled={loading}>
                    {loading ? 'Sending...' : 'Confirm Application'}
                </Button>
            </DialogActions>
        </Dialog>
    );
};

export default ApplicationModal;
