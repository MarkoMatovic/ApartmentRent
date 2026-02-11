import React, { useState } from 'react';
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
    IconButton,
    Menu,
    MenuItem,
    ListItemIcon,
    Chip,
} from '@mui/material';
import {
    MoreVert as MoreVertIcon,
    Archive as ArchiveIcon,
    Unarchive as UnarchiveIcon,
    VolumeOff as MuteIcon,
    VolumeUp as UnmuteIcon,
    Block as BlockIcon,
    Delete as DeleteIcon,
    Report as ReportIcon,
} from '@mui/icons-material';
import { format, parseISO } from 'date-fns';
import { useTranslation } from 'react-i18next';
import { Conversation } from '../../shared/types/message';

interface MessageListProps {
    conversations: Conversation[];
    selectedConversationId?: number;
    onSelectConversation: (conversationId: number) => void;
    onArchive?: (otherUserId: number) => void;
    onUnarchive?: (otherUserId: number) => void;
    onMute?: (otherUserId: number) => void;
    onUnmute?: (otherUserId: number) => void;
    onBlock?: (otherUserId: number) => void;
    onUnblock?: (otherUserId: number) => void;
    onDelete?: (otherUserId: number) => void;
    onReport?: (otherUserId: number) => void;
}

export const MessageList: React.FC<MessageListProps> = ({
    conversations,
    selectedConversationId,
    onSelectConversation,
    onArchive,
    onUnarchive,
    onMute,
    onUnmute,
    onBlock,
    onUnblock,
    onDelete,
    onReport,
}) => {
    const { t } = useTranslation(['chat']);
    const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
    const [selectedConv, setSelectedConv] = useState<Conversation | null>(null);

    const handleMenuOpen = (event: React.MouseEvent<HTMLElement>, conversation: Conversation) => {
        event.stopPropagation();
        setAnchorEl(event.currentTarget);
        setSelectedConv(conversation);
    };

    const handleMenuClose = () => {
        setAnchorEl(null);
        setSelectedConv(null);
    };

    const handleAction = (action: () => void) => {
        action();
        handleMenuClose();
    };

    return (
        <>
            <List sx={{ width: '100%', bgcolor: 'background.paper', p: 0 }}>
                {conversations.map((conversation, index) => (
                    <React.Fragment key={conversation.otherUserId}>
                        <ListItemButton
                            selected={selectedConversationId === conversation.otherUserId}
                            onClick={() => onSelectConversation(conversation.otherUserId)}
                            sx={{
                                opacity: conversation.isArchived ? 0.6 : 1,
                            }}
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
                                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                                        <Typography
                                            variant="subtitle2"
                                            fontWeight={conversation.unreadCount > 0 ? 'bold' : 'normal'}
                                        >
                                            {conversation.otherUserName}
                                        </Typography>
                                        {conversation.isMuted && (
                                            <Chip
                                                icon={<MuteIcon fontSize="small" />}
                                                label={t('chat:muted')}
                                                size="small"
                                                sx={{ height: 20, fontSize: '0.7rem' }}
                                            />
                                        )}
                                        {conversation.isArchived && (
                                            <Chip
                                                icon={<ArchiveIcon fontSize="small" />}
                                                label={t('chat:archived')}
                                                size="small"
                                                sx={{ height: 20, fontSize: '0.7rem' }}
                                            />
                                        )}
                                    </Box>
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
                            <IconButton
                                edge="end"
                                onClick={(e) => handleMenuOpen(e, conversation)}
                                size="small"
                            >
                                <MoreVertIcon />
                            </IconButton>
                        </ListItemButton>
                        {index < conversations.length - 1 && <Divider />}
                    </React.Fragment>
                ))}
            </List>

            <Menu
                anchorEl={anchorEl}
                open={Boolean(anchorEl)}
                onClose={handleMenuClose}
            >
                {selectedConv?.isArchived ? (
                    <MenuItem onClick={() => handleAction(() => onUnarchive?.(selectedConv.otherUserId))}>
                        <ListItemIcon>
                            <UnarchiveIcon fontSize="small" />
                        </ListItemIcon>
                        <Typography variant="inherit">{t('chat:unarchive')}</Typography>
                    </MenuItem>
                ) : (
                    <MenuItem onClick={() => handleAction(() => onArchive?.(selectedConv!.otherUserId))}>
                        <ListItemIcon>
                            <ArchiveIcon fontSize="small" />
                        </ListItemIcon>
                        <Typography variant="inherit">{t('chat:archive')}</Typography>
                    </MenuItem>
                )}

                {selectedConv?.isMuted ? (
                    <MenuItem onClick={() => handleAction(() => onUnmute?.(selectedConv.otherUserId))}>
                        <ListItemIcon>
                            <UnmuteIcon fontSize="small" />
                        </ListItemIcon>
                        <Typography variant="inherit">{t('chat:unmute')}</Typography>
                    </MenuItem>
                ) : (
                    <MenuItem onClick={() => handleAction(() => onMute?.(selectedConv!.otherUserId))}>
                        <ListItemIcon>
                            <MuteIcon fontSize="small" />
                        </ListItemIcon>
                        <Typography variant="inherit">{t('chat:mute')}</Typography>
                    </MenuItem>
                )}

                {selectedConv?.isBlocked ? (
                    <MenuItem onClick={() => handleAction(() => onUnblock?.(selectedConv.otherUserId))}>
                        <ListItemIcon>
                            <BlockIcon fontSize="small" />
                        </ListItemIcon>
                        <Typography variant="inherit">{t('chat:unblockUser')}</Typography>
                    </MenuItem>
                ) : (
                    <MenuItem onClick={() => handleAction(() => onBlock?.(selectedConv!.otherUserId))}>
                        <ListItemIcon>
                            <BlockIcon fontSize="small" />
                        </ListItemIcon>
                        <Typography variant="inherit">{t('chat:blockUser')}</Typography>
                    </MenuItem>
                )}

                <Divider />

                <MenuItem onClick={() => handleAction(() => onReport?.(selectedConv!.otherUserId))}>
                    <ListItemIcon>
                        <ReportIcon fontSize="small" color="warning" />
                    </ListItemIcon>
                    <Typography variant="inherit" color="warning.main">{t('chat:reportAbuse')}</Typography>
                </MenuItem>

                <MenuItem onClick={() => handleAction(() => onDelete?.(selectedConv!.otherUserId))}>
                    <ListItemIcon>
                        <DeleteIcon fontSize="small" color="error" />
                    </ListItemIcon>
                    <Typography variant="inherit" color="error">{t('chat:deleteConversation')}</Typography>
                </MenuItem>
            </Menu>
        </>
    );
};
