import React from 'react';
import {
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    Button,
    Typography,
    Box,
} from '@mui/material';
import WarningAmberIcon from '@mui/icons-material/WarningAmber';
import { useTranslation } from 'react-i18next';

interface RejectionDialogProps {
    open: boolean;
    apartmentTitle: string;
    onKeep: () => void;
    onRemove: () => void;
}

const RejectionDialog: React.FC<RejectionDialogProps> = ({
    open,
    apartmentTitle,
    onKeep,
    onRemove,
}) => {
    const { t } = useTranslation('applications');

    return (
        <Dialog open={open} maxWidth="sm" fullWidth>
            <DialogTitle>
                <Box display="flex" alignItems="center" gap={1}>
                    <WarningAmberIcon color="warning" />
                    <Typography variant="h6">{t('rejectedTitle')}</Typography>
                </Box>
            </DialogTitle>
            <DialogContent>
                <Typography variant="body1" gutterBottom>
                    {t('rejectedBody', { title: apartmentTitle })}
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                    {t('keepApartmentQuestion')}
                </Typography>
            </DialogContent>
            <DialogActions sx={{ px: 3, pb: 2, gap: 1 }}>
                <Button
                    variant="outlined"
                    color="error"
                    onClick={onRemove}
                >
                    {t('removeApartment')}
                </Button>
                <Button
                    variant="contained"
                    onClick={onKeep}
                >
                    {t('keepApartment')}
                </Button>
            </DialogActions>
        </Dialog>
    );
};

export default RejectionDialog;
