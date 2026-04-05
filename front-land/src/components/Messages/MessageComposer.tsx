import React, { useState, useRef } from 'react';
import {
    TextField,
    IconButton,
    Paper,
    Box,
    Chip,
    Alert,
    Tooltip,
} from '@mui/material';
import {
    Send as SendIcon,
    AttachFile as AttachFileIcon,
    Close as CloseIcon,
    Star as StarIcon,
    StarBorder as StarBorderIcon,
} from '@mui/icons-material';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../../shared/context/AuthContext';

interface MessageComposerProps {
    onSendMessage: (content: string, isSuperLike?: boolean) => void;
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
    const { user } = useAuth();
    const [message, setMessage] = useState('');
    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [fileError, setFileError] = useState<string | null>(null);
    const [isSuperLike, setIsSuperLike] = useState(false);
    const fileInputRef = useRef<HTMLInputElement>(null);

    const isPremium = !!(user?.hasPersonalAnalytics || user?.hasLandlordAnalytics);
    const tokenBalance = user?.tokenBalance ?? 0;
    const canSuperLike = isPremium && tokenBalance > 0;

    const handleSend = () => {
        if (selectedFile && onSendFile) {
            onSendFile(selectedFile);
            setSelectedFile(null);
            setFileError(null);
        } else if (message.trim()) {
            onSendMessage(message.trim(), isSuperLike && canSuperLike);
            setMessage('');
            setIsSuperLike(false);
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

        if (file.size > MAX_FILE_SIZE) {
            setFileError(t('chat:fileTooLarge'));
            return;
        }

        if (!ALLOWED_FILE_TYPES.includes(file.type)) {
            setFileError(t('chat:invalidFileType'));
            return;
        }

        setSelectedFile(file);
        setFileError(null);
        setMessage('');
    };

    const handleRemoveFile = () => {
        setSelectedFile(null);
        setFileError(null);
        if (fileInputRef.current) {
            fileInputRef.current.value = '';
        }
    };

    const superLikeTooltip = !isPremium
        ? 'Super-Like is a premium feature'
        : tokenBalance === 0
        ? 'No tokens remaining'
        : `Super-Like (${tokenBalance} token${tokenBalance !== 1 ? 's' : ''} remaining)`;

    return (
        <Paper
            elevation={2}
            sx={{
                p: 2,
                display: 'flex',
                flexDirection: 'column',
                gap: 1,
                ...(isSuperLike && canSuperLike && {
                    border: '1px solid',
                    borderColor: 'warning.main',
                    background: (theme) =>
                        theme.palette.mode === 'dark'
                            ? 'rgba(255,193,7,0.05)'
                            : 'rgba(255,193,7,0.04)',
                }),
            }}
        >
            {fileError && (
                <Alert severity="error" onClose={() => setFileError(null)}>
                    {fileError}
                </Alert>
            )}

            {isSuperLike && canSuperLike && (
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                    <StarIcon fontSize="small" sx={{ color: 'warning.main' }} />
                    <Box component="span" sx={{ fontSize: 12, color: 'warning.main', fontWeight: 500 }}>
                        Super-Like · {tokenBalance} token{tokenBalance !== 1 ? 's' : ''} remaining
                    </Box>
                </Box>
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

            <Box sx={{ display: 'flex', gap: 1, alignItems: 'flex-end' }}>
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

                {/* Super-Like toggle */}
                <Tooltip title={superLikeTooltip} arrow>
                    <span>
                        <IconButton
                            color={isSuperLike && canSuperLike ? 'warning' : 'default'}
                            onClick={() => canSuperLike && setIsSuperLike((v) => !v)}
                            disabled={disabled || !canSuperLike || !!selectedFile}
                            size="small"
                        >
                            {isSuperLike && canSuperLike ? <StarIcon /> : <StarBorderIcon />}
                        </IconButton>
                    </span>
                </Tooltip>

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
                    color={isSuperLike && canSuperLike ? 'warning' : 'primary'}
                    onClick={handleSend}
                    disabled={disabled || (!message.trim() && !selectedFile)}
                >
                    <SendIcon />
                </IconButton>
            </Box>
        </Paper>
    );
};
