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
    // File upload properties
    fileUrl?: string;
    fileName?: string;
    fileSize?: number;
    fileType?: string;
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
    // Conversation settings
    isArchived: boolean;
    isMuted: boolean;
    isBlocked: boolean;
}

export interface SendMessageRequest {
    receiverId: number;
    content: string;
}

export interface ReportMessageRequest {
    messageId: number;
    reportedUserId: number;
    reason: string;
}

export interface ChatActionRequest {
    otherUserId: number;
}

export interface ReportedMessage {
    reportId: number;
    messageId: number;
    messageText: string;
    reportedByUserId: number;
    reportedByUserName: string;
    reportedUserId: number;
    reportedUserName: string;
    reason: string;
    status: string;
    createdDate: string;
    reviewedByAdminId?: number;
    reviewedByAdminName?: string;
    reviewedDate?: string;
    adminNotes?: string;
}
