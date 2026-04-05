import React, { useState } from 'react';
import { Dialog, DialogTitle, DialogContent, DialogActions, Button, Typography, Alert, FormControlLabel, Checkbox, Box, Paper } from '@mui/material';
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
                title: isPriority ? 'Priority Application Sent!' : 'Application Sent',
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
        <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth PaperProps={{
            sx: { borderRadius: 3, p: 1 }
        }}>
            <DialogTitle sx={{ fontWeight: 'bold' }}>Prijavi se za: {apartmentTitle}</DialogTitle>
            <DialogContent>
                {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
                <Typography variant="body1" sx={{ mb: 3 }}>
                    Da li ste sigurni da želite da pošaljete prijavu za ovaj stan? Stanodavac će odmah biti obavešten.
                </Typography>

                <Paper 
                    variant="outlined" 
                    sx={{ 
                        p: 2, 
                        mb: 2, 
                        borderRadius: 2,
                        cursor: 'pointer',
                        transition: 'all 0.2s',
                        borderColor: isPriority ? 'primary.main' : 'divider',
                        bgcolor: isPriority ? 'action.hover' : 'transparent',
                        '&:hover': { bgcolor: 'action.hover' }
                    }}
                    onClick={() => setIsPriority(!isPriority)}
                >
                    <FormControlLabel
                        control={
                            <Checkbox 
                                checked={isPriority} 
                                onChange={(e) => setIsPriority(e.target.checked)}
                                icon={<StarIcon color="disabled" />}
                                checkedIcon={<StarIcon color="primary" />}
                            />
                        }
                        label={
                            <Box>
                                <Typography variant="subtitle1" fontWeight="bold">
                                    Prioritetna prijava (Premium)
                                </Typography>
                                <Typography variant="body2" color="text.secondary">
                                    Vaša prijava će se pojaviti na samom vrhu kod stanodavca i biti istaknuta zvezdicom. (Cena: €2.00)
                                </Typography>
                            </Box>
                        }
                        sx={{ m: 0, width: '100%', alignItems: 'flex-start' }}
                    />
                </Paper>
            </DialogContent>
            <DialogActions sx={{ px: 3, pb: 2 }}>
                <Button onClick={onClose} disabled={loading} color="inherit">Otkaži</Button>
                <Button 
                    onClick={handleApply} 
                    variant="contained" 
                    disabled={loading}
                    sx={{ 
                        px: 4, 
                        borderRadius: 2,
                        boxShadow: isPriority ? '0 4px 14px 0 rgba(0,118,255,0.39)' : 'none'
                    }}
                >
                    {loading ? 'Slanje...' : 'Potvrdi prijavu'}
                </Button>
            </DialogActions>
        </Dialog>
    );
};

export default ApplicationModal;
