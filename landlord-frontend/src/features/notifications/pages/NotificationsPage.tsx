import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Box,
  Typography,
  List,
  ListItem,
  ListItemText,
  ListItemButton,
  Chip,
  IconButton,
} from '@mui/material';
import { CheckCircle as CheckCircleIcon } from '@mui/icons-material';
import { notificationsService } from '../api/notificationsService';
import { useAuthStore } from '@/features/auth/store/authStore';
import { LoadingSpinner } from '@/shared/components/ui/LoadingSpinner';
import { ErrorAlert } from '@/shared/components/ui/ErrorAlert';
import { EmptyState } from '@/shared/components/ui/EmptyState';
import { AppLayout } from '@/shared/components/layout/AppLayout';
import { NotificationDto } from '../types';

const NotificationsPage = () => {
  const { userId } = useAuthStore();
  const queryClient = useQueryClient();

  const {
    data: notifications,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey: ['notifications', userId],
    queryFn: () => notificationsService.getUserNotifications(userId || 0),
    enabled: !!userId && userId > 0,
  });

  const markAsReadMutation = useMutation({
    mutationFn: (notificationId: number) =>
      notificationsService.markAsRead(notificationId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications', userId] });
    },
  });

  const handleMarkAsRead = (notification: NotificationDto) => {
    if (!notification.isRead) {
      markAsReadMutation.mutate(notification.id);
    }
  };

  if (!userId || userId === 0) {
    return (
      <AppLayout>
        <ErrorAlert
          message="User ID is not available. Please ensure you are logged in correctly."
          title="Authentication Error"
        />
      </AppLayout>
    );
  }

  if (isLoading) {
    return (
      <AppLayout>
        <LoadingSpinner fullScreen />
      </AppLayout>
    );
  }

  if (error) {
    return (
      <AppLayout>
        <ErrorAlert
          message={error instanceof Error ? error.message : 'Failed to load notifications'}
          onRetry={() => refetch()}
        />
      </AppLayout>
    );
  }

  if (!notifications || notifications.length === 0) {
    return (
      <AppLayout>
        <EmptyState message="No notifications" />
      </AppLayout>
    );
  }

  return (
    <AppLayout>
      <Typography variant="h4" component="h1" gutterBottom>
        Notifications
      </Typography>
      <List>
        {notifications.map((notification) => (
          <ListItem
            key={notification.id}
            secondaryAction={
              !notification.isRead && (
                <IconButton
                  edge="end"
                  onClick={() => handleMarkAsRead(notification)}
                  disabled={markAsReadMutation.isPending}
                >
                  <CheckCircleIcon />
                </IconButton>
              )
            }
            sx={{
              bgcolor: notification.isRead ? 'background.paper' : 'action.selected',
              mb: 1,
              borderRadius: 1,
            }}
          >
            <ListItemButton>
              <ListItemText
                primary={
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Typography variant="h6">{notification.title}</Typography>
                    {!notification.isRead && (
                      <Chip label="New" color="primary" size="small" />
                    )}
                  </Box>
                }
                secondary={
                  <>
                    <Typography variant="body2" color="text.secondary">
                      {notification.message}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {new Date(notification.createdDate).toLocaleString()}
                    </Typography>
                  </>
                }
              />
            </ListItemButton>
          </ListItem>
        ))}
      </List>
    </AppLayout>
  );
};

export default NotificationsPage;

