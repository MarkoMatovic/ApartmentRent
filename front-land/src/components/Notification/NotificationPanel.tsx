import React from 'react';
import {
  Paper,
  Menu,
  Typography,
  Box,
  IconButton,
  List,
  ListItem,
  ListItemText,
  Divider,
  Button,
} from '@mui/material';
import { Close as CloseIcon, CheckCircle as CheckCircleIcon } from '@mui/icons-material';
import { useTranslation } from 'react-i18next';
import { useNotifications } from '../../shared/context/NotificationContext';
import { format } from 'date-fns';

interface NotificationPanelProps {
  anchorEl: HTMLElement | null;
  open: boolean;
  onClose: () => void;
}

const NotificationPanel: React.FC<NotificationPanelProps> = ({ anchorEl, open, onClose }) => {
  const { t } = useTranslation('common');
  const { notifications, markAsRead, markAllAsRead, clearNotification, unreadCount } =
    useNotifications();

  return (
    <Menu
      anchorEl={anchorEl}
      open={open}
      onClose={onClose}
      anchorOrigin={{
        vertical: 'bottom',
        horizontal: 'right',
      }}
      transformOrigin={{
        vertical: 'top',
        horizontal: 'right',
      }}
      PaperProps={{
        sx: {
          width: 400,
          maxHeight: 500,
          mt: 1,
        },
      }}
    >
      <Box sx={{ p: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Typography variant="h6">{t('notifications')}</Typography>
        <Box>
          {unreadCount > 0 && (
            <Button size="small" onClick={markAllAsRead}>
              {t('markAsRead')}
            </Button>
          )}
          <IconButton size="small" onClick={onClose}>
            <CloseIcon />
          </IconButton>
        </Box>
      </Box>
      <Divider />
      {notifications.length === 0 ? (
        <Box sx={{ p: 3, textAlign: 'center' }}>
          <Typography variant="body2" color="text.secondary">
            {t('noNotifications')}
          </Typography>
        </Box>
      ) : (
        <List sx={{ maxHeight: 400, overflow: 'auto' }}>
          {notifications.map((notification) => (
            <ListItem
              key={notification.id}
              sx={{
                bgcolor: notification.read ? 'transparent' : 'action.hover',
                '&:hover': { bgcolor: 'action.selected' },
              }}
            >
              <ListItemText
                primary={
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Typography variant="subtitle2">{notification.title}</Typography>
                    {!notification.read && (
                      <CheckCircleIcon sx={{ fontSize: 16, color: 'primary.main' }} />
                    )}
                  </Box>
                }
                secondary={
                  <>
                    <Typography variant="body2" color="text.secondary">
                      {notification.message}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {format(notification.timestamp, 'PPp')}
                    </Typography>
                  </>
                }
              />
              <IconButton
                size="small"
                onClick={() => {
                  if (!notification.read) {
                    markAsRead(notification.id);
                  } else {
                    clearNotification(notification.id);
                  }
                }}
              >
                <CloseIcon fontSize="small" />
              </IconButton>
            </ListItem>
          ))}
        </List>
      )}
    </Menu>
  );
};

export default NotificationPanel;

