import React, { useState, useRef } from 'react';
import {
    TextField,
    IconButton,
    Paper,
    Box,
    Chip,
    Alert,
} from '@mui/material';
import {
    Send as SendIcon,
    AttachFile as AttachFileIcon,
    Close as CloseIcon,
} from '@mui/icons-material';
import { useTranslation } from 'react-i18next';

interface MessageComposerProps {
    onSendMessage: (content: string) => void;
    onSendFile?: (file: File) => void;
    disabled?: boolean;
}

const MAX_FILE_SIZE = 3 * 1024 * 1024; // 3MB
const ALLOWED_FILE_TYPES = [
    'image/jpeg',
    'image/jpg',
    'image/png',
    'image/gif',
    'application/pdf',
    'application/msword',
    'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    'application/vnd.ms-excel',
    'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
];

export const MessageComposer: React.FC<MessageComposerProps> = ({
    onSendMessage,
    onSendFile,
    disabled = false,
}) => {
    const { t } = useTranslation(['chat']);
    const [message, setMessage] = useState('');
    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [fileError, setFileError] = useState<string | null>(null);
    const fileInputRef = useRef<HTMLInputElement>(null);

    const handleSend = () => {
        if (selectedFile && onSendFile) {
            onSendFile(selectedFile);
            setSelectedFile(null);
            setFileError(null);
        } else if (message.trim()) {
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

    const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
        const file = event.target.files?.[0];
        if (!file) return;

        // Validate file size
        if (file.size > MAX_FILE_SIZE) {
            setFileError(t('chat:fileTooLarge'));
            return;
        }

        // Validate file type
        if (!ALLOWED_FILE_TYPES.includes(file.type)) {
            setFileError(t('chat:invalidFileType'));
            return;
        }

        setSelectedFile(file);
        setFileError(null);
        setMessage(''); // Clear message when file is selected
    };

    const handleRemoveFile = () => {
        setSelectedFile(null);
        setFileError(null);
        if (fileInputRef.current) {
            fileInputRef.current.value = '';
        }
    };

    return (
        <Paper
            elevation={2}
            sx={{
                p: 2,
                display: 'flex',
                flexDirection: 'column',
                gap: 1,
            }}
        >
            {fileError && (
                <Alert severity="error" onClose={() => setFileError(null)}>
                    {fileError}
                </Alert>
            )}

            {selectedFile && (
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Chip
                        label={`${selectedFile.name} (${(selectedFile.size / 1024).toFixed(1)} KB)`}
                        onDelete={handleRemoveFile}
                        deleteIcon={<CloseIcon />}
                        color="primary"
                        variant="outlined"
                    />
                </Box>
            )}

            <Box sx={{ display: 'flex', gap: 2, alignItems: 'flex-end' }}>
                <TextField
                    fullWidth
                    multiline
                    maxRows={4}
                    value={message}
                    onChange={(e) => setMessage(e.target.value)}
                    onKeyPress={handleKeyPress}
                    placeholder={selectedFile ? t('chat:fileSelected') : t('chat:typeMessage')}
                    disabled={disabled || !!selectedFile}
                    variant="outlined"
                    size="small"
                />
                <input
                    ref={fileInputRef}
                    type="file"
                    accept={ALLOWED_FILE_TYPES.join(',')}
                    onChange={handleFileSelect}
                    style={{ display: 'none' }}
                />
                {onSendFile && (
                    <IconButton
                        color="default"
                        onClick={() => fileInputRef.current?.click()}
                        disabled={disabled || !!selectedFile}
                    >
                        <AttachFileIcon />
                    </IconButton>
                )}
                <IconButton
                    color="primary"
                    onClick={handleSend}
                    disabled={disabled || (!message.trim() && !selectedFile)}
                >
                    <SendIcon />
                </IconButton>
            </Box>
        </Paper>
    );
};
