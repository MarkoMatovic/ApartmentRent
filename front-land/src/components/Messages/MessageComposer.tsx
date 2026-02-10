import React, { useState } from 'react';
import {
    TextField,
    IconButton,
    Paper,
} from '@mui/material';
import { Send as SendIcon } from '@mui/icons-material';
import { useTranslation } from 'react-i18next';

interface MessageComposerProps {
    onSendMessage: (content: string) => void;
    disabled?: boolean;
}

export const MessageComposer: React.FC<MessageComposerProps> = ({
    onSendMessage,
    disabled = false,
}) => {
    const { t } = useTranslation(['messages']);
    const [message, setMessage] = useState('');

    const handleSend = () => {
        if (message.trim()) {
            onSendMessage(message.trim());
            setMessage('');
        }
    };

    const handleKeyPress = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            handleSend();
        }
    };

    return (
        <Paper
            elevation={2}
            sx={{
                p: 2,
                display: 'flex',
                gap: 2,
                alignItems: 'flex-end',
            }}
        >
            <TextField
                fullWidth
                multiline
                maxRows={4}
                value={message}
                onChange={(e) => setMessage(e.target.value)}
                onKeyPress={handleKeyPress}
                placeholder={t('messages:typeMessage')}
                disabled={disabled}
                variant="outlined"
                size="small"
            />
            <IconButton
                color="primary"
                onClick={handleSend}
                disabled={disabled || !message.trim()}
            >
                <SendIcon />
            </IconButton>
        </Paper>
    );
};
