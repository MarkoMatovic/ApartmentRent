import { apiClient } from './client';
import { Message, Conversation, SendMessageRequest } from '../types/message';
import { jwtDecode } from 'jwt-decode';

interface JwtPayload {
    userId: string;
    [key: string]: any;
}

const getUserId = (): number => {
    const token = localStorage.getItem('authToken'); // Changed from 'token' to 'authToken'
    if (!token) throw new Error('No auth token');
    const decoded = jwtDecode<JwtPayload>(token);
    return parseInt(decoded.userId);
};

export const messagesApi = {
    /**
     * Get all conversations for the current user
     */
    getConversations: async (): Promise<Conversation[]> => {
        try {
            const userId = getUserId();
            console.log('Fetching conversations for userId:', userId);
            const response = await apiClient.get(`/api/v1/messages/user/${userId}`);
            console.log('Conversations response:', response.data);
            return response.data;
        } catch (error: any) {
            console.error('Error fetching conversations:', error);
            console.error('Error response:', error.response?.data);
            console.error('Error status:', error.response?.status);
            throw error;
        }
    },

    /**
     * Get messages in a specific conversation
     */
    getConversationMessages: async (conversationId: number): Promise<Message[]> => {
        // Need to get userId and otherUserId from conversation
        // For now, use the conversation endpoint
        const response = await apiClient.get(`/api/v1/messages/conversation`, {
            params: { userId1: getUserId(), userId2: conversationId }
        });
        return response.data.messages || [];
    },

    /**
     * Get messages with a specific user
     */
    getMessagesWithUser: async (userId: number): Promise<Message[]> => {
        const response = await apiClient.get(`/api/v1/messages/conversation`, {
            params: { userId1: getUserId(), userId2: userId }
        });
        return response.data.messages || [];
    },

    /**
     * Send a new message
     */
    sendMessage: async (data: SendMessageRequest): Promise<Message> => {
        const userId = getUserId();
        const response = await apiClient.post('/api/v1/messages/send', {
            senderId: userId,
            receiverId: data.receiverId,
            messageText: data.content
        });
        return response.data;
    },

    /**
     * Mark messages as read
     */
    markAsRead: async (messageId: number): Promise<void> => {
        await apiClient.put(`/api/v1/messages/mark-read/${messageId}`);
    },

    /**
     * Get unread message count
     */
    getUnreadCount: async (): Promise<number> => {
        const userId = getUserId();
        const response = await apiClient.get(`/api/v1/messages/unread-count/${userId}`);
        return response.data;
    },

    // Conversation Settings
    archiveConversation: async (otherUserId: number): Promise<void> => {
        const userId = getUserId();
        await apiClient.post(`/api/v1/messages/archive?userId=${userId}`, { otherUserId });
    },

    unarchiveConversation: async (otherUserId: number): Promise<void> => {
        const userId = getUserId();
        await apiClient.post(`/api/v1/messages/unarchive`, { userId, otherUserId });
    },

    muteConversation: async (otherUserId: number): Promise<void> => {
        const userId = getUserId();
        await apiClient.post(`/api/v1/messages/mute`, { userId, otherUserId });
    },

    unmuteConversation: async (otherUserId: number): Promise<void> => {
        const userId = getUserId();
        await apiClient.post(`/api/v1/messages/unmute`, { userId, otherUserId });
    },

    blockUser: async (otherUserId: number): Promise<void> => {
        const userId = getUserId();
        await apiClient.post(`/api/v1/messages/block`, { userId, otherUserId });
    },

    unblockUser: async (otherUserId: number): Promise<void> => {
        const userId = getUserId();
        await apiClient.post(`/api/v1/messages/unblock`, { userId, otherUserId });
    },

    deleteConversation: async (otherUserId: number): Promise<void> => {
        const userId = getUserId();
        await apiClient.delete(`/api/v1/messages/delete-conversation?userId=${userId}&otherUserId=${otherUserId}`);
    },

    // File Upload
    uploadFile: async (file: File): Promise<{ fileUrl: string }> => {
        const userId = getUserId();
        const formData = new FormData();
        formData.append('file', file);
        const response = await apiClient.post(`/api/v1/messages/upload?userId=${userId}`, formData, {
            headers: { 'Content-Type': 'multipart/form-data' }
        });
        return response.data;
    },

    // Search
    searchMessages: async (query: string): Promise<Message[]> => {
        const userId = getUserId();
        const response = await apiClient.get(`/api/v1/messages/search?userId=${userId}&query=${encodeURIComponent(query)}`);
        return response.data;
    },

    // Report Abuse
    reportAbuse: async (messageId: number, reportedUserId: number, reason: string): Promise<void> => {
        const userId = getUserId();
        await apiClient.post(`/api/v1/messages/report`, { userId, reportedUserId, messageId, reason });
    },
};

