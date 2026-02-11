import React, { useState } from 'react';
import {
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    Button,
    TextField,
    Alert,
} from '@mui/material';
import { useTranslation } from 'react-i18next';

interface ReportAbuseDialogProps {
    open: boolean;
    onClose: () => void;
    onSubmit: (reason: string) => void;
    userName?: string;
}

export const ReportAbuseDialog: React.FC<ReportAbuseDialogProps> = ({
    open,
    onClose,
    onSubmit,
    userName,
}) => {
    const { t } = useTranslation(['chat']);
    const [reason, setReason] = useState('');
    const [error, setError] = useState('');

    const handleSubmit = () => {
        if (reason.trim().length < 10) {
            setError(t('chat:reportReasonTooShort'));
            return;
        }

        onSubmit(reason.trim());
        setReason('');
        setError('');
        onClose();
    };

    const handleClose = () => {
        setReason('');
        setError('');
        onClose();
    };

    return (
        <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
            <DialogTitle>{t('chat:reportAbuse')}</DialogTitle>
            <DialogContent>
                {userName && (
                    <Alert severity="warning" sx={{ mb: 2 }}>
                        {t('chat:reportingUser', { userName })}
                    </Alert>
                )}
                <TextField
                    autoFocus
                    fullWidth
                    multiline
                    rows={4}
                    label={t('chat:reportReason')}
                    value={reason}
                    onChange={(e) => setReason(e.target.value)}
                    error={!!error}
                    helperText={error || t('chat:reportReasonHelper')}
                    placeholder={t('chat:reportReasonPlaceholder')}
                />
            </DialogContent>
            <DialogActions>
                <Button onClick={handleClose}>{t('chat:cancel')}</Button>
                <Button
                    onClick={handleSubmit}
                    variant="contained"
                    color="warning"
                    disabled={reason.trim().length < 10}
                >
                    {t('chat:submit')}
                </Button>
            </DialogActions>
        </Dialog>
    );
};
