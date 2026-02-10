import React, { useEffect, useRef } from 'react';
import {
    Box,
    Paper,
    Typography,
    Avatar,
} from '@mui/material';
import { format, parseISO } from 'date-fns';
import { Message } from '../../shared/types/message';
import { useAuth } from '../../shared/context/AuthContext';

interface MessageThreadProps {
    messages: Message[];
}

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
