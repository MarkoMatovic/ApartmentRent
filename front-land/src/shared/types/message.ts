export interface Message {
    messageId: number;
    conversationId: number;
    senderId: number;
    senderName: string;
    senderProfilePicture?: string;
    receiverId: number;
    receiverName: string;
    messageText: string; // Changed from 'content' to match backend
    sentAt: string;
    isRead: boolean;
}

export interface Conversation {
    otherUserId: number;
    otherUserName: string;
    otherUserProfilePicture?: string;
    lastMessage?: {
        messageId: number;
        senderId: number;
        receiverId: number;
        messageText: string;
        sentAt: string;
        isRead: boolean;
    };
    unreadCount: number;
}

export interface SendMessageRequest {
    receiverId: number;
    content: string;
}
