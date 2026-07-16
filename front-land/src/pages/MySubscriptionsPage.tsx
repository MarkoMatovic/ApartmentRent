import React, { useEffect, useState } from 'react';
import {
  Container, Typography, Box, Card, CardContent, CardActions,
  Chip, Button, Divider, Grid, CircularProgress, Alert, Dialog,
  DialogTitle, DialogContent, DialogActions,
} from '@mui/material';
import AnalyticsIcon from '@mui/icons-material/Analytics';
import TokenIcon from '@mui/icons-material/Token';
import HomeIcon from '@mui/icons-material/Home';
import RocketLaunchIcon from '@mui/icons-material/RocketLaunch';
import MarkEmailReadIcon from '@mui/icons-material/MarkEmailRead';
import { apiClient } from '../shared/api/client';
import { useNotifications } from '../shared/context/NotificationContext';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

interface SubscriptionStatus {
  hasAnalytics: boolean;
  tokenBalance: number;
  listingCredits: number;
  boostedUntil: string | null;
  priorityInboxUntil: string | null;
  roleName: string | null;
  isBoosted: boolean;
  hasPriorityInbox: boolean;
}

const formatDate = (iso: string | null) =>
  iso ? new Date(iso).toLocaleDateString('sr-RS', { day: '2-digit', month: 'long', year: 'numeric' }) : null;

const MySubscriptionsPage: React.FC = () => {
  const { t } = useTranslation('subscriptions');
  const [status, setStatus] = useState<SubscriptionStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [cancelOpen, setCancelOpen] = useState(false);
  const [cancelling, setCancelling] = useState(false);
  const { addNotification } = useNotifications();
  const navigate = useNavigate();

  const ActiveChip = () => <Chip label={t('statusActive')} color="success" size="small" />;
  const InactiveChip = () => <Chip label={t('statusInactive')} color="default" size="small" />;

  const fetchStatus = async () => {
    try {
      const res = await apiClient.get('/api/payments/my-status');
      setStatus(res.data);
    } catch {
      addNotification({ title: t('common:error'), message: t('errorLoadStatus'), type: 'error' });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchStatus(); }, []);

  const cancelAnalytics = async () => {
    setCancelling(true);
    try {
      await apiClient.post('/api/payments/cancel-analytics');
      addNotification({ title: t('deactivateSuccessTitle'), message: t('deactivateSuccessMsg'), type: 'success' });
      setCancelOpen(false);
      await fetchStatus();
    } catch {
      addNotification({ title: t('deactivateErrorTitle'), message: t('deactivateErrorMsg'), type: 'error' });
    } finally {
      setCancelling(false);
    }
  };

  if (loading) return <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}><CircularProgress /></Box>;

  return (
    <Container maxWidth="md" sx={{ py: 5 }}>
      <Typography variant="h4" fontWeight="bold" gutterBottom>{t('mySubsTitle')}</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 4 }}>
        {t('mySubsSubtitle')}
      </Typography>

      {!status ? (
        <Alert severity="error">{t('errorLoadData')}</Alert>
      ) : (
        <Grid container spacing={3}>

          {/* Analytics */}
          <Grid item xs={12} sm={6}>
            <Card variant="outlined" sx={{ height: '100%' }}>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1.5 }}>
                  <AnalyticsIcon color={status.hasAnalytics ? 'primary' : 'disabled'} />
                  <Typography fontWeight="bold">{t('analyticsCardTitle')}</Typography>
                  {status.hasAnalytics ? <ActiveChip /> : <InactiveChip />}
                </Box>
                <Typography variant="body2" color="text.secondary">
                  {status.hasAnalytics
                    ? t('analyticsActiveDesc')
                    : t('analyticsInactiveDesc')}
                </Typography>
                {status.roleName && (
                  <Chip label={status.roleName} size="small" variant="outlined" sx={{ mt: 1.5 }} />
                )}
              </CardContent>
              {status.hasAnalytics && (
                <CardActions>
                  <Button size="small" color="error" onClick={() => setCancelOpen(true)}>
                    {t('btnDeactivate')}
                  </Button>
                </CardActions>
              )}
              {!status.hasAnalytics && (
                <CardActions>
                  <Button size="small" onClick={() => navigate('/pricing')}>{t('btnBuyService')}</Button>
                </CardActions>
              )}
            </Card>
          </Grid>

          {/* Tokens */}
          <Grid item xs={12} sm={6}>
            <Card variant="outlined" sx={{ height: '100%' }}>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1.5 }}>
                  <TokenIcon color={status.tokenBalance > 0 ? 'primary' : 'disabled'} />
                  <Typography fontWeight="bold">{t('tokensCardTitle')}</Typography>
                  <Chip label={t('tokensChip', { count: status.tokenBalance })} size="small"
                    color={status.tokenBalance > 0 ? 'primary' : 'default'} />
                </Box>
                <Typography variant="body2" color="text.secondary">
                  {t('tokensCardDesc')}
                </Typography>
              </CardContent>
              <CardActions>
                <Button size="small" onClick={() => navigate('/pricing')}>{t('btnBuyMore')}</Button>
              </CardActions>
            </Card>
          </Grid>

          {/* Listing Credits */}
          <Grid item xs={12} sm={6}>
            <Card variant="outlined" sx={{ height: '100%' }}>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1.5 }}>
                  <HomeIcon color={status.listingCredits > 0 ? 'primary' : 'disabled'} />
                  <Typography fontWeight="bold">{t('listingCreditsTitle')}</Typography>
                  <Chip label={t('listingCreditsChip', { count: status.listingCredits })} size="small"
                    color={status.listingCredits > 0 ? 'primary' : 'default'} />
                </Box>
                <Typography variant="body2" color="text.secondary">
                  {t('listingCreditsDesc')}
                </Typography>
              </CardContent>
              <CardActions>
                <Button size="small" onClick={() => navigate('/pricing')}>{t('btnBuyMore')}</Button>
              </CardActions>
            </Card>
          </Grid>

          {/* Boost profila */}
          <Grid item xs={12} sm={6}>
            <Card variant="outlined" sx={{ height: '100%' }}>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1.5 }}>
                  <RocketLaunchIcon color={status.isBoosted ? 'primary' : 'disabled'} />
                  <Typography fontWeight="bold">{t('boostCardTitle')}</Typography>
                  {status.isBoosted ? <ActiveChip /> : <InactiveChip />}
                </Box>
                {status.boostedUntil && status.isBoosted ? (
                  <Typography variant="body2" color="text.secondary">
                    {t('boostActiveUntil')} <strong>{formatDate(status.boostedUntil)}</strong>
                  </Typography>
                ) : (
                  <Typography variant="body2" color="text.secondary">
                    {t('boostInactiveDesc')}
                  </Typography>
                )}
              </CardContent>
              <CardActions>
                <Button size="small" onClick={() => navigate('/pricing')}>
                  {status.isBoosted ? t('btnExtend') : t('btnActivate')}
                </Button>
              </CardActions>
            </Card>
          </Grid>

          {/* Priority Inbox */}
          <Grid item xs={12} sm={6}>
            <Card variant="outlined" sx={{ height: '100%' }}>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1.5 }}>
                  <MarkEmailReadIcon color={status.hasPriorityInbox ? 'primary' : 'disabled'} />
                  <Typography fontWeight="bold">{t('priorityCardTitle')}</Typography>
                  {status.hasPriorityInbox ? <ActiveChip /> : <InactiveChip />}
                </Box>
                {status.priorityInboxUntil && status.hasPriorityInbox ? (
                  <Typography variant="body2" color="text.secondary">
                    {t('priorityActiveUntil')} <strong>{formatDate(status.priorityInboxUntil)}</strong>
                  </Typography>
                ) : (
                  <Typography variant="body2" color="text.secondary">
                    {t('priorityInactiveDesc')}
                  </Typography>
                )}
              </CardContent>
              <CardActions>
                <Button size="small" onClick={() => navigate('/pricing')}>
                  {status.hasPriorityInbox ? t('btnExtend') : t('btnActivate')}
                </Button>
              </CardActions>
            </Card>
          </Grid>

        </Grid>
      )}

      <Divider sx={{ my: 4 }} />
      <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
        <Button variant="outlined" onClick={() => navigate('/istorija-placanja')}>
          {t('btnPaymentHistory')}
        </Button>
        <Button variant="outlined" onClick={() => navigate('/pricing')}>
          {t('btnBuyServices')}
        </Button>
        <Button variant="text" href="/politika-povracaja" size="small" sx={{ ml: 'auto' }}>
          {t('btnRefundPolicy')}
        </Button>
      </Box>

      {/* Cancel analytics confirmation dialog */}
      <Dialog open={cancelOpen} onClose={() => setCancelOpen(false)} maxWidth="xs">
        <DialogTitle>{t('cancelAnalyticsTitle')}</DialogTitle>
        <DialogContent>
          <Typography variant="body2"
            dangerouslySetInnerHTML={{ __html: t('cancelAnalyticsBody') }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCancelOpen(false)}>{t('common:cancel')}</Button>
          <Button color="error" onClick={cancelAnalytics} disabled={cancelling}>
            {cancelling ? <CircularProgress size={20} /> : t('btnCancelConfirm')}
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
};

export default MySubscriptionsPage;
