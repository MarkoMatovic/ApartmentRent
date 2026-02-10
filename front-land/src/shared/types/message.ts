export interface Message {
    messageId: number;
    conversationId: number;
    senderId: number;
    senderName: string;
    senderProfilePicture?: string;
    receiverId: number;
    receiverName: string;
    content: string;
    sentAt: string;
    isRead: boolean;
}

export interface Conversation {
    conversationId: number;
    otherUserId: number;
    otherUserName: string;
    otherUserProfilePicture?: string;
    lastMessage?: string;
    lastMessageAt?: string;
    unreadCount: number;
}

export interface SendMessageRequest {
    receiverId: number;
    content: string;
}
