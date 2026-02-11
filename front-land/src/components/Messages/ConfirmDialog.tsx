import React from 'react';
import {
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    DialogContentText,
    Button,
} from '@mui/material';
import { useTranslation } from 'react-i18next';

interface ConfirmDialogProps {
    open: boolean;
    onClose: () => void;
    onConfirm: () => void;
    title: string;
    message: string;
    confirmText?: string;
    confirmColor?: 'error' | 'warning' | 'primary';
}

export const ConfirmDialog: React.FC<ConfirmDialogProps> = ({
    open,
    onClose,
    onConfirm,
    title,
    message,
    confirmText,
    confirmColor = 'error',
}) => {
    const { t } = useTranslation(['chat']);

    const handleConfirm = () => {
        onConfirm();
        onClose();
    };

    return (
        <Dialog open={open} onClose={onClose}>
            <DialogTitle>{title}</DialogTitle>
            <DialogContent>
                <DialogContentText>{message}</DialogContentText>
            </DialogContent>
            <DialogActions>
                <Button onClick={onClose}>{t('chat:cancel')}</Button>
                <Button onClick={handleConfirm} color={confirmColor} variant="contained">
                    {confirmText || t('chat:confirm')}
                </Button>
            </DialogActions>
        </Dialog>
    );
};
