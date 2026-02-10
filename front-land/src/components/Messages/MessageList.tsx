import React from 'react';
import {
    Box,
    List,
    ListItemButton,
    ListItemAvatar,
    ListItemText,
    Avatar,
    Badge,
    Typography,
    Divider,
} from '@mui/material';
import { format, parseISO } from 'date-fns';
import { Conversation } from '../../shared/types/message';

interface MessageListProps {
    conversations: Conversation[];
    selectedConversationId?: number;
    onSelectConversation: (conversationId: number) => void;
}

export const MessageList: React.FC<MessageListProps> = ({
    conversations,
    selectedConversationId,
    onSelectConversation,
}) => {
    return (
        <List sx={{ width: '100%', bgcolor: 'background.paper', p: 0 }}>
            {conversations.map((conversation, index) => (
                <React.Fragment key={conversation.otherUserId}>
                    <ListItemButton
                        selected={selectedConversationId === conversation.otherUserId}
                        onClick={() => onSelectConversation(conversation.otherUserId)}
                    >
                        <ListItemAvatar>
                            <Badge
                                badgeContent={conversation.unreadCount}
                                color="primary"
                                invisible={conversation.unreadCount === 0}
                            >
                                <Avatar
                                    src={conversation.otherUserProfilePicture}
                                    alt={conversation.otherUserName}
                                >
                                    {conversation.otherUserName.charAt(0).toUpperCase()}
                                </Avatar>
                            </Badge>
                        </ListItemAvatar>
                        <ListItemText
                            primary={
                                <Typography
                                    variant="subtitle2"
                                    fontWeight={conversation.unreadCount > 0 ? 'bold' : 'normal'}
                                >
                                    {conversation.otherUserName}
                                </Typography>
                            }
                            secondary={
                                <Box>
                                    <Typography
                                        variant="body2"
                                        color="text.secondary"
                                        noWrap
                                        sx={{ maxWidth: 200 }}
                                    >
                                        {conversation.lastMessage?.messageText || 'No messages yet'}
                                    </Typography>
                                    {conversation.lastMessage?.sentAt && (
                                        <Typography variant="caption" color="text.secondary">
                                            {format(parseISO(conversation.lastMessage.sentAt), 'MMM d, HH:mm')}
                                        </Typography>
                                    )}
                                </Box>
                            }
                        />
                    </ListItemButton>
                    {index < conversations.length - 1 && <Divider />}
                </React.Fragment>
            ))}
        </List>
    );
};
