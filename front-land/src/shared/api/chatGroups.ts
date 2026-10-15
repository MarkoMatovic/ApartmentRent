import apiClient from './client';

export interface ChatGroupMember {
  userId: number;
  name: string;
  profilePicture?: string | null;
}

export interface ChatGroupMessage {
  messageId: number;
  groupId: number;
  senderId: number;
  senderName?: string | null;
  senderProfilePicture?: string | null;
  messageText: string;
  sentAt: string;
}

export interface ChatGroup {
  groupId: number;
  /** Everybody in the group, the signed-in user included. */
  members: ChatGroupMember[];
  lastMessage?: ChatGroupMessage | null;
  unreadCount: number;
}

export interface ChatGroupMessagesPage {
  totalCount: number;
  page: number;
  pageSize: number;
  messages: ChatGroupMessage[];
}

const base = '/api/v1/chat-groups';

export const chatGroupsApi = {
  getMine: async (): Promise<ChatGroup[]> => {
    const { data } = await apiClient.get<ChatGroup[]>(base);
    return data;
  },

  /** Opens (or creates) the group of me, my roommate and `otherUserId` - usually the landlord. */
  open: async (otherUserId: number): Promise<ChatGroup> => {
    const { data } = await apiClient.post<ChatGroup>(`${base}/open`, { otherUserId });
    return data;
  },

  getMessages: async (groupId: number, page = 1, pageSize = 50): Promise<ChatGroupMessagesPage> => {
    const { data } = await apiClient.get<ChatGroupMessagesPage>(`${base}/${groupId}/messages`, {
      params: { page, pageSize },
    });
    return data;
  },

  sendMessage: async (groupId: number, messageText: string): Promise<ChatGroupMessage> => {
    const { data } = await apiClient.post<ChatGroupMessage>(`${base}/${groupId}/messages`, { messageText });
    return data;
  },

  markRead: async (groupId: number): Promise<void> => {
    await apiClient.post(`${base}/${groupId}/read`);
  },

  leave: async (groupId: number): Promise<void> => {
    await apiClient.post(`${base}/${groupId}/leave`);
  },
};

/** "Marko, Lazar": everybody except me, as a title for the group. */
export const groupTitle = (group: Pick<ChatGroup, 'members'>, myUserId: number): string =>
  group.members
    .filter(m => m.userId !== myUserId)
    .map(m => m.name)
    .filter(Boolean)
    .join(', ');
