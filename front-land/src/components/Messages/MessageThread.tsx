import React, { useEffect, useRef } from 'react';
import {
    Box,
    Paper,
    Typography,
    Avatar,
    Link,
    Card,
    CardMedia,
} from '@mui/material';
import {
    InsertDriveFile as FileIcon,
    PictureAsPdf as PdfIcon,
    Description as DocIcon,
} from '@mui/icons-material';
import { format, parseISO } from 'date-fns';
import { Message } from '../../shared/types/message';
import { useAuth } from '../../shared/context/AuthContext';

interface MessageThreadProps {
    messages: Message[];
}

const getFileIcon = (fileType?: string) => {
    if (!fileType) return <FileIcon />;
    if (fileType.includes('pdf')) return <PdfIcon />;
    if (fileType.includes('doc') || fileType.includes('document')) return <DocIcon />;
    return <FileIcon />;
};

const isImageFile = (fileType?: string) => {
    return fileType?.startsWith('image/');
};

const formatFileSize = (bytes?: number) => {
    if (!bytes) return '';
    const kb = bytes / 1024;
    if (kb < 1024) return `${kb.toFixed(1)} KB`;
    return `${(kb / 1024).toFixed(1)} MB`;
};

export const MessageThread: React.FC<MessageThreadProps> = ({ messages }) => {
    const { user } = useAuth();
    const messagesEndRef = useRef<HTMLDivElement>(null);

    const scrollToBottom = () => {
        messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    };

    useEffect(() => {
        scrollToBottom();
    }, [messages]);

    return (
        <Box
            sx={{
                flexGrow: 1,
                overflow: 'auto',
                p: 2,
                display: 'flex',
                flexDirection: 'column',
                gap: 2,
                background: (theme) =>
                    theme.palette.mode === 'dark'
                        ? '#0d1418'
                        : '#efeae2',
                position: 'relative',
                '&::before': {
                    content: '""',
                    position: 'absolute',
                    top: 0,
                    left: 0,
                    right: 0,
                    bottom: 0,
                    opacity: (theme) => theme.palette.mode === 'dark' ? 0.03 : 0.06,
                    backgroundImage: (theme) => theme.palette.mode === 'dark'
                        ? `url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='100' height='100' viewBox='0 0 100 100'%3E%3Cg fill-rule='evenodd'%3E%3Cg fill='%23ffffff' fill-opacity='1'%3E%3Cpath opacity='.5' d='M96 95h4v1h-4v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4H0v-1h15v-9H0v-1h15v-9H0v-1h15v-9H0v-1h15v-9H0v-1h15v-9H0v-1h15v-9H0v-1h15v-9H0v-1h15v-9H0v-1h15V0h1v15h9V0h1v15h9V0h1v15h9V0h1v15h9V0h1v15h9V0h1v15h9V0h1v15h9V0h1v15h9V0h1v15h4v1h-4v9h4v1h-4v9h4v1h-4v9h4v1h-4v9h4v1h-4v9h4v1h-4v9h4v1h-4v9h4v1h-4v9zm-1 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-9-10h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm9-10v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-9-10h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm9-10v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-9-10h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm9-10v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-9-10h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9z'/%3E%3Cpath d='M6 5V0H5v5H0v1h5v94h1V6h94V5H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")`
                        : `url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='100' height='100' viewBox='0 0 100 100'%3E%3Cg fill-rule='evenodd'%3E%3Cg fill='%23000000' fill-opacity='1'%3E%3Cpath opacity='.5' d='M96 95h4v1h-4v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4h-9v4h-1v-4H0v-1h15v-9H0v-1h15v-9H0v-1h15v-9H0v-1h15v-9H0v-1h15v-9H0v-1h15v-9H0v-1h15v-9H0v-1h15v-9H0v-1h15V0h1v15h9V0h1v15h9V0h1v15h9V0h1v15h9V0h1v15h9V0h1v15h9V0h1v15h9V0h1v15h9V0h1v15h4v1h-4v9h4v1h-4v9h4v1h-4v9h4v1h-4v9h4v1h-4v9h4v1h-4v9h4v1h-4v9h4v1h-4v9zm-1 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-9-10h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm9-10v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-9-10h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm9-10v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-9-10h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm9-10v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-10 0v-9h-9v9h9zm-9-10h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9zm10 0h9v-9h-9v9z'/%3E%3Cpath d='M6 5V0H5v5H0v1h5v94h1V6h94V5H6z'/%3E%3C/g%3E%3C/g%3E%3C/svg%3E")`,
                    pointerEvents: 'none',
                },
            }}
        >
            {messages.map((message) => {
                const isOwnMessage = message.senderId === user?.userId;

                return (
                    <Box
                        key={message.messageId}
                        sx={{
                            display: 'flex',
                            flexDirection: isOwnMessage ? 'row-reverse' : 'row',
                            gap: 1,
                            alignItems: 'flex-start',
                        }}
                    >
                        <Avatar
                            src={message.senderProfilePicture}
                            alt={message.senderName}
                            sx={{ width: 32, height: 32 }}
                        >
                            {message.senderName.charAt(0).toUpperCase()}
                        </Avatar>

                        <Box
                            sx={{
                                maxWidth: '70%',
                                display: 'flex',
                                flexDirection: 'column',
                                alignItems: isOwnMessage ? 'flex-end' : 'flex-start',
                            }}
                        >
                            <Typography variant="caption" color="text.secondary" sx={{ mb: 0.5 }}>
                                {message.senderName}
                            </Typography>

                            {/* Message Text */}
                            {message.messageText && (
                                <Paper
                                    elevation={1}
                                    sx={{
                                        p: 1.5,
                                        bgcolor: isOwnMessage ? 'primary.main' : 'background.paper',
                                        color: isOwnMessage ? 'primary.contrastText' : 'text.primary',
                                    }}
                                >
                                    <Typography variant="body2">
                                        {message.messageText}
                                    </Typography>
                                </Paper>
                            )}

                            {/* File Attachment */}
                            {message.fileUrl && (
                                <Box sx={{ mt: message.messageText ? 1 : 0 }}>
                                    {isImageFile(message.fileType) ? (
                                        <Card sx={{ maxWidth: 300 }}>
                                            <CardMedia
                                                component="img"
                                                image={message.fileUrl}
                                                alt={message.fileName || 'Image'}
                                                sx={{ maxHeight: 200, objectFit: 'contain' }}
                                            />
                                            <Box sx={{ p: 1, bgcolor: 'background.paper' }}>
                                                <Typography variant="caption" noWrap>
                                                    {message.fileName}
                                                </Typography>
                                                <Typography variant="caption" color="text.secondary" display="block">
                                                    {formatFileSize(message.fileSize)}
                                                </Typography>
                                            </Box>
                                        </Card>
                                    ) : (
                                        <Paper
                                            elevation={1}
                                            sx={{
                                                p: 1.5,
                                                display: 'flex',
                                                alignItems: 'center',
                                                gap: 1,
                                                bgcolor: 'background.paper',
                                            }}
                                        >
                                            {getFileIcon(message.fileType)}
                                            <Box sx={{ flexGrow: 1, minWidth: 0 }}>
                                                <Link
                                                    href={message.fileUrl}
                                                    download={message.fileName}
                                                    underline="hover"
                                                    target="_blank"
                                                    rel="noopener noreferrer"
                                                >
                                                    <Typography variant="body2" noWrap>
                                                        {message.fileName}
                                                    </Typography>
                                                </Link>
                                                <Typography variant="caption" color="text.secondary">
                                                    {formatFileSize(message.fileSize)}
                                                </Typography>
                                            </Box>
                                        </Paper>
                                    )}
                                </Box>
                            )}

                            <Typography variant="caption" color="text.secondary" sx={{ mt: 0.5 }}>
                                {format(parseISO(message.sentAt), 'HH:mm')}
                            </Typography>
                        </Box>
                    </Box>
                );
            })}
            <div ref={messagesEndRef} />
        </Box>
    );
};
