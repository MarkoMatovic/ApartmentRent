import axios from 'axios';

const API_BASE_URL = 'https://localhost:7092/api/v1';

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
  senderId: number;
  receiverId: number;
  messageText: string;
}

export const messagesApi = {
  getConversation: async (userId1: number, userId2: number, page = 1, pageSize = 50): Promise<ConversationMessagesDto> => {
    const response = await axios.get(`${API_BASE_URL}/messages/conversation`, {
      params: { userId1, userId2, page, pageSize },
      headers: {
        Authorization: `Bearer ${localStorage.getItem('authToken')}`
      }
    });
    return response.data;
  },

  getUserConversations: async (userId: number): Promise<ConversationDto[]> => {
    const response = await axios.get(`${API_BASE_URL}/messages/user/${userId}`, {
      headers: {
        Authorization: `Bearer ${localStorage.getItem('authToken')}`
      }
    });
    return response.data;
  },

  sendMessage: async (input: SendMessageInputDto): Promise<MessageDto> => {
    const response = await axios.post(`${API_BASE_URL}/messages/send`, input, {
      headers: {
        Authorization: `Bearer ${localStorage.getItem('authToken')}`
      }
    });
    return response.data;
  },

  markAsRead: async (messageId: number): Promise<void> => {
    await axios.put(`${API_BASE_URL}/messages/mark-read/${messageId}`, null, {
      headers: {
        Authorization: `Bearer ${localStorage.getItem('authToken')}`
      }
    });
  },

  getUnreadCount: async (userId: number): Promise<number> => {
    const response = await axios.get(`${API_BASE_URL}/messages/unread-count/${userId}`, {
      headers: {
        Authorization: `Bearer ${localStorage.getItem('authToken')}`
      }
    });
    return response.data;
  }
};
