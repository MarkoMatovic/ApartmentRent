import React, { useState } from 'react';
import {
    Container,
    Box,
    Paper,
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
            const selectedConversation = conversations?.find(c => c.conversationId === selectedConversationId);
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

    const handleSelectConversation = (conversationId: number) => {
        setSelectedConversationId(conversationId);

        // Mark as read
        messagesApi.markAsRead(conversationId).then(() => {
            queryClient.invalidateQueries({ queryKey: ['conversations'] });
        });
    };

    if (conversationsLoading) {
        return (
            <Container maxWidth="xl" sx={{ py: 4, display: 'flex', justifyContent: 'center' }}>
                <CircularProgress />
            </Container>
        );
    }

    return (
        <Container maxWidth="xl" sx={{ py: 4 }}>
            <Typography variant="h4" component="h1" gutterBottom sx={{ mb: 3 }}>
                {t('messages:messages')}
            </Typography>

            <Paper elevation={3} sx={{ height: 'calc(100vh - 200px)', display: 'flex' }}>
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
                    <Grid item xs={12} md={8} sx={{ display: 'flex', flexDirection: 'column' }}>
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
            </Paper>
        </Container>
    );
};

export default MessagesPage;
