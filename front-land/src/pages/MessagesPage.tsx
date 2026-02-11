import React, { useState } from 'react';
import {
    Box,
    Typography,
    Grid,
    CircularProgress,
    Alert,
    Snackbar,
} from '@mui/material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { messagesApi } from '../shared/api/messages';
import { MessageList } from '../components/Messages/MessageList';
import { MessageThread } from '../components/Messages/MessageThread';
import { MessageComposer } from '../components/Messages/MessageComposer';
import { ReportAbuseDialog } from '../components/Messages/ReportAbuseDialog';
import { ConfirmDialog } from '../components/Messages/ConfirmDialog';

const MessagesPage: React.FC = () => {
    const { t } = useTranslation(['common', 'chat']);
    const queryClient = useQueryClient();
    const [selectedConversationId, setSelectedConversationId] = useState<number | null>(null);
    const [reportDialogOpen, setReportDialogOpen] = useState(false);
    const [confirmDialog, setConfirmDialog] = useState<{
        open: boolean;
        title: string;
        message: string;
        action: () => void;
    }>({ open: false, title: '', message: '', action: () => { } });
    const [snackbar, setSnackbar] = useState<{ open: boolean; message: string; severity: 'success' | 'error' }>({
        open: false,
        message: '',
        severity: 'success',
    });

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

    const uploadFileMutation = useMutation({
        mutationFn: async (file: File) => {
            const selectedConversation = conversations?.find(c => c.otherUserId === selectedConversationId);
            if (!selectedConversation) throw new Error('No conversation selected');

            // Upload file first
            const uploadedFile = await messagesApi.uploadFile(file);

            // Then send message with file info
            return messagesApi.sendMessage({
                receiverId: selectedConversation.otherUserId,
                content: uploadedFile.fileName || '',
            });
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['conversation-messages', selectedConversationId] });
            queryClient.invalidateQueries({ queryKey: ['conversations'] });
        },
        onError: () => {
            setSnackbar({ open: true, message: t('chat:fileUploadFailed'), severity: 'error' });
        },
    });

    const archiveMutation = useMutation({
        mutationFn: messagesApi.archiveConversation,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['conversations'] });
            setSnackbar({ open: true, message: t('chat:conversationArchived'), severity: 'success' });
        },
    });

    const unarchiveMutation = useMutation({
        mutationFn: messagesApi.unarchiveConversation,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['conversations'] });
            setSnackbar({ open: true, message: t('chat:conversationUnarchived'), severity: 'success' });
        },
    });

    const muteMutation = useMutation({
        mutationFn: messagesApi.muteConversation,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['conversations'] });
            setSnackbar({ open: true, message: t('chat:conversationMuted'), severity: 'success' });
        },
    });

    const unmuteMutation = useMutation({
        mutationFn: messagesApi.unmuteConversation,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['conversations'] });
            setSnackbar({ open: true, message: t('chat:conversationUnmuted'), severity: 'success' });
        },
    });

    const blockMutation = useMutation({
        mutationFn: messagesApi.blockUser,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['conversations'] });
            setSnackbar({ open: true, message: t('chat:userBlocked'), severity: 'success' });
        },
    });

    const unblockMutation = useMutation({
        mutationFn: messagesApi.unblockUser,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['conversations'] });
            setSnackbar({ open: true, message: t('chat:userUnblocked'), severity: 'success' });
        },
    });

    const deleteMutation = useMutation({
        mutationFn: messagesApi.deleteConversation,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['conversations'] });
            setSelectedConversationId(null);
            setSnackbar({ open: true, message: t('chat:conversationDeleted'), severity: 'success' });
        },
    });

    const reportMutation = useMutation({
        mutationFn: ({ messageId, reportedUserId, reason }: { messageId: number; reportedUserId: number; reason: string }) =>
            messagesApi.reportAbuse(messageId, reportedUserId, reason),
        onSuccess: () => {
            setSnackbar({ open: true, message: t('chat:reportSubmitted'), severity: 'success' });
        },
    });

    const handleSelectConversation = (otherUserId: number) => {
        setSelectedConversationId(otherUserId);

        // Mark as read
        const conversation = conversations?.find(c => c.otherUserId === otherUserId);
        if (conversation?.lastMessage?.messageId) {
            messagesApi.markAsRead(conversation.lastMessage.messageId).then(() => {
                queryClient.invalidateQueries({ queryKey: ['conversations'] });
            });
        }
    };

    const handleBlock = (otherUserId: number) => {
        const conversation = conversations?.find(c => c.otherUserId === otherUserId);
        setConfirmDialog({
            open: true,
            title: t('chat:blockUser'),
            message: t('chat:blockUserConfirm', { userName: conversation?.otherUserName }),
            action: () => blockMutation.mutate(otherUserId),
        });
    };

    const handleDelete = (otherUserId: number) => {
        const conversation = conversations?.find(c => c.otherUserId === otherUserId);
        setConfirmDialog({
            open: true,
            title: t('chat:deleteConversation'),
            message: t('chat:deleteConversationConfirm', { userName: conversation?.otherUserName }),
            action: () => deleteMutation.mutate(otherUserId),
        });
    };

    const handleReport = (otherUserId: number) => {
        setSelectedConversationId(otherUserId);
        setReportDialogOpen(true);
    };

    const handleReportSubmit = (reason: string) => {
        const conversation = conversations?.find(c => c.otherUserId === selectedConversationId);
        if (conversation?.lastMessage) {
            reportMutation.mutate({
                messageId: conversation.lastMessage.messageId,
                reportedUserId: selectedConversationId!,
                reason,
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

    const selectedConversation = conversations?.find(c => c.otherUserId === selectedConversationId);

    return (
        <Box
            sx={{
                position: 'fixed',
                top: 64,
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
                    {t('chat:messages')}
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
                                onArchive={(id) => archiveMutation.mutate(id)}
                                onUnarchive={(id) => unarchiveMutation.mutate(id)}
                                onMute={(id) => muteMutation.mutate(id)}
                                onUnmute={(id) => unmuteMutation.mutate(id)}
                                onBlock={handleBlock}
                                onUnblock={(id) => unblockMutation.mutate(id)}
                                onDelete={handleDelete}
                                onReport={handleReport}
                            />
                        ) : (
                            <Box sx={{ p: 3, textAlign: 'center' }}>
                                <Typography color="text.secondary">
                                    {t('chat:noConversations')}
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
                                            onSendFile={(file) => uploadFileMutation.mutate(file)}
                                            disabled={sendMessageMutation.isPending || uploadFileMutation.isPending}
                                        />
                                    </>
                                ) : (
                                    <Alert severity="error" sx={{ m: 2 }}>
                                        {t('chat:errorLoadingMessages')}
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
                                    {t('chat:selectConversation')}
                                </Typography>
                            </Box>
                        )}
                    </Grid>
                </Grid>
            </Box>

            {/* Dialogs */}
            <ReportAbuseDialog
                open={reportDialogOpen}
                onClose={() => setReportDialogOpen(false)}
                onSubmit={handleReportSubmit}
                userName={selectedConversation?.otherUserName}
            />

            <ConfirmDialog
                open={confirmDialog.open}
                onClose={() => setConfirmDialog({ ...confirmDialog, open: false })}
                onConfirm={confirmDialog.action}
                title={confirmDialog.title}
                message={confirmDialog.message}
            />

            {/* Snackbar */}
            <Snackbar
                open={snackbar.open}
                autoHideDuration={3000}
                onClose={() => setSnackbar({ ...snackbar, open: false })}
            >
                <Alert severity={snackbar.severity} onClose={() => setSnackbar({ ...snackbar, open: false })}>
                    {snackbar.message}
                </Alert>
            </Snackbar>
        </Box>
    );
};

export default MessagesPage;
