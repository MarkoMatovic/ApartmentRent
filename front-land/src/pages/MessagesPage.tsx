import React, { useState } from 'react';
import {
    Box,
    Typography,
    Grid,
    CircularProgress,
    Alert,
} from '@mui/material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { messagesApi } from '../shared/api/messages';
import { MessageList } from '../components/Messages/MessageList';
import { MessageThread } from '../components/Messages/MessageThread';
import { MessageComposer } from '../components/Messages/MessageComposer';

const MessagesPage: React.FC = () => {
    const { t } = useTranslation(['common', 'messages']);
    const queryClient = useQueryClient();
    const [selectedConversationId, setSelectedConversationId] = useState<number | null>(null);

    const { data: conversations, isLoading: conversationsLoading } = useQuery({
        queryKey: ['conversations'],
        queryFn: messagesApi.getConversations,
    });

    const { data: messages, isLoading: messagesLoading } = useQuery({
        queryKey: ['conversation-messages', selectedConversationId],
        queryFn: () => messagesApi.getConversationMessages(selectedConversationId!),
        enabled: !!selectedConversationId,
    });

    const sendMessageMutation = useMutation({
        mutationFn: (content: string) => {
            const selectedConversation = conversations?.find(c => c.otherUserId === selectedConversationId);
            if (!selectedConversation) throw new Error('No conversation selected');

            return messagesApi.sendMessage({
                receiverId: selectedConversation.otherUserId,
                content,
            });
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['conversation-messages', selectedConversationId] });
            queryClient.invalidateQueries({ queryKey: ['conversations'] });
        },
    });

    const handleSelectConversation = (otherUserId: number) => {
        setSelectedConversationId(otherUserId);

        // Mark as read - need to find the last message ID
        const conversation = conversations?.find(c => c.otherUserId === otherUserId);
        if (conversation?.lastMessage?.messageId) {
            messagesApi.markAsRead(conversation.lastMessage.messageId).then(() => {
                queryClient.invalidateQueries({ queryKey: ['conversations'] });
            });
        }
    };

    if (conversationsLoading) {
        return (
            <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh' }}>
                <CircularProgress />
            </Box>
        );
    }

    return (
        <Box
            sx={{
                position: 'fixed',
                top: 64, // Header height
                left: 0,
                right: 0,
                bottom: 0,
                display: 'flex',
                flexDirection: 'column',
                bgcolor: 'background.default',
            }}
        >
            {/* Title Bar */}
            <Box sx={{ px: 3, py: 2, borderBottom: 1, borderColor: 'divider', bgcolor: 'background.paper' }}>
                <Typography variant="h5" component="h1">
                    {t('messages:messages')}
                </Typography>
            </Box>

            {/* Chat Container */}
            <Box sx={{ flexGrow: 1, display: 'flex', overflow: 'hidden' }}>
                <Grid container sx={{ height: '100%' }}>
                    {/* Conversations List */}
                    <Grid
                        item
                        xs={12}
                        md={4}
                        sx={{
                            borderRight: { md: 1 },
                            borderColor: 'divider',
                            overflow: 'auto',
                            height: '100%',
                            bgcolor: 'background.paper',
                        }}
                    >
                        {conversations && conversations.length > 0 ? (
                            <MessageList
                                conversations={conversations}
                                selectedConversationId={selectedConversationId || undefined}
                                onSelectConversation={handleSelectConversation}
                            />
                        ) : (
                            <Box sx={{ p: 3, textAlign: 'center' }}>
                                <Typography color="text.secondary">
                                    {t('messages:noConversations')}
                                </Typography>
                            </Box>
                        )}
                    </Grid>

                    {/* Message Thread */}
                    <Grid
                        item
                        xs={12}
                        md={8}
                        sx={{
                            display: 'flex',
                            flexDirection: 'column',
                            height: '100%',
                            bgcolor: 'background.default',
                        }}
                    >
                        {selectedConversationId ? (
                            <>
                                {messagesLoading ? (
                                    <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', flexGrow: 1 }}>
                                        <CircularProgress />
                                    </Box>
                                ) : messages ? (
                                    <>
                                        <MessageThread messages={messages} />
                                        <MessageComposer
                                            onSendMessage={(content) => sendMessageMutation.mutate(content)}
                                            disabled={sendMessageMutation.isPending}
                                        />
                                    </>
                                ) : (
                                    <Alert severity="error" sx={{ m: 2 }}>
                                        {t('messages:errorLoadingMessages')}
                                    </Alert>
                                )}
                            </>
                        ) : (
                            <Box
                                sx={{
                                    display: 'flex',
                                    justifyContent: 'center',
                                    alignItems: 'center',
                                    flexGrow: 1,
                                }}
                            >
                                <Typography color="text.secondary">
                                    {t('messages:selectConversation')}
                                </Typography>
                            </Box>
                        )}
                    </Grid>
                </Grid>
            </Box>
        </Box>
    );
};

export default MessagesPage;
