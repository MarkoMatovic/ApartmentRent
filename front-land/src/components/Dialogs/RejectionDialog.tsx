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
    return (
        <Dialog open={open} maxWidth="sm" fullWidth>
            <DialogTitle>
                <Box display="flex" alignItems="center" gap={1}>
                    <WarningAmberIcon color="warning" />
                    <Typography variant="h6">Application Not Approved</Typography>
                </Box>
            </DialogTitle>
            <DialogContent>
                <Typography variant="body1" gutterBottom>
                    Your application for <strong>"{apartmentTitle}"</strong> has been rejected by the landlord.
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                    Would you like to keep this apartment in your search results?
                </Typography>
            </DialogContent>
            <DialogActions sx={{ px: 3, pb: 2, gap: 1 }}>
                <Button
                    variant="outlined"
                    color="error"
                    onClick={onRemove}
                >
                    No, remove it
                </Button>
                <Button
                    variant="contained"
                    onClick={onKeep}
                >
                    Yes, keep it
                </Button>
            </DialogActions>
        </Dialog>
    );
};

export default RejectionDialog;
