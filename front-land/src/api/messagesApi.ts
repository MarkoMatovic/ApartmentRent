import apiClient from '../shared/api/client';

export interface MessageDto {
  messageId: number;
  senderId: number;
  receiverId: number;
  messageText: string;
  sentAt: string;
  isRead: boolean;
  senderName?: string;
  receiverName?: string;
  senderProfilePicture?: string;
  receiverProfilePicture?: string;
}

export interface ConversationDto {
  otherUserId: number;
  otherUserName?: string;
  otherUserProfilePicture?: string;
  lastMessage?: MessageDto;
  unreadCount: number;
}

export interface ConversationMessagesDto {
  totalCount: number;
  page: number;
  pageSize: number;
  messages: MessageDto[];
}

export interface SendMessageInputDto {
  receiverId: number;
  messageText: string;
  isSuperLike?: boolean;
}

export const messagesApi = {
  getConversation: async (userId1: number, userId2: number, page = 1, pageSize = 50): Promise<ConversationMessagesDto> => {
    const response = await apiClient.get('/api/v1/messages/conversation', {
      params: { userId1, userId2, page, pageSize },
    });
    return response.data;
  },

  getUserConversations: async (userId: number): Promise<ConversationDto[]> => {
    const response = await apiClient.get(`/api/v1/messages/user/${userId}`);
    return response.data;
  },

  sendMessage: async (input: SendMessageInputDto): Promise<MessageDto> => {
    const response = await apiClient.post('/api/v1/messages/send', input);
    return response.data;
  },

  markAsRead: async (messageId: number): Promise<void> => {
    await apiClient.put(`/api/v1/messages/mark-read/${messageId}`, null);
  },

  getUnreadCount: async (userId: number): Promise<number> => {
    const response = await apiClient.get(`/api/v1/messages/unread-count/${userId}`);
    return response.data;
  }
};
