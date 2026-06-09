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

const ActiveChip = () => <Chip label="Aktivno" color="success" size="small" />;
const InactiveChip = () => <Chip label="Neaktivno" color="default" size="small" />;

const MySubscriptionsPage: React.FC = () => {
  const [status, setStatus] = useState<SubscriptionStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [cancelOpen, setCancelOpen] = useState(false);
  const [cancelling, setCancelling] = useState(false);
  const { addNotification } = useNotifications();
  const navigate = useNavigate();

  const fetchStatus = async () => {
    try {
      const res = await apiClient.get('/api/payments/my-status');
      setStatus(res.data);
    } catch {
      addNotification({ title: 'Greška', message: 'Nije moguće učitati status pretplata.', type: 'error' });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchStatus(); }, []);

  const cancelAnalytics = async () => {
    setCancelling(true);
    try {
      await apiClient.post('/api/payments/cancel-analytics');
      addNotification({ title: 'Deaktivacija uspešna', message: 'Analitika je deaktivirana. Nalog je vraćen na osnovni plan.', type: 'success' });
      setCancelOpen(false);
      await fetchStatus();
    } catch {
      addNotification({ title: 'Greška', message: 'Deaktivacija nije uspela. Pokušajte ponovo.', type: 'error' });
    } finally {
      setCancelling(false);
    }
  };

  if (loading) return <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}><CircularProgress /></Box>;

  return (
    <Container maxWidth="md" sx={{ py: 5 }}>
      <Typography variant="h4" fontWeight="bold" gutterBottom>Moje pretplate i usluge</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 4 }}>
        Pregled svih aktivnih premium usluga na vašem nalogu. Sve kupovine su jednokratne — nema
        automatskog obnavljanja.
      </Typography>

      {!status ? (
        <Alert severity="error">Nije moguće učitati podatke. Pokušajte osvežiti stranicu.</Alert>
      ) : (
        <Grid container spacing={3}>

          {/* Analytics */}
          <Grid item xs={12} sm={6}>
            <Card variant="outlined" sx={{ height: '100%' }}>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1.5 }}>
                  <AnalyticsIcon color={status.hasAnalytics ? 'primary' : 'disabled'} />
                  <Typography fontWeight="bold">Analitika</Typography>
                  {status.hasAnalytics ? <ActiveChip /> : <InactiveChip />}
                </Box>
                <Typography variant="body2" color="text.secondary">
                  {status.hasAnalytics
                    ? 'Pristup naprednoj analitici pretrage, praćenju pregleda i ML predviđanju cena.'
                    : 'Kupite analitičku pretplatu na stranici Cenovnik za napredne uvide.'}
                </Typography>
                {status.roleName && (
                  <Chip label={status.roleName} size="small" variant="outlined" sx={{ mt: 1.5 }} />
                )}
              </CardContent>
              {status.hasAnalytics && (
                <CardActions>
                  <Button size="small" color="error" onClick={() => setCancelOpen(true)}>
                    Deaktiviraj
                  </Button>
                </CardActions>
              )}
              {!status.hasAnalytics && (
                <CardActions>
                  <Button size="small" onClick={() => navigate('/pricing')}>Kupi</Button>
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
                  <Typography fontWeight="bold">Tokeni</Typography>
                  <Chip label={`${status.tokenBalance} tokena`} size="small"
                    color={status.tokenBalance > 0 ? 'primary' : 'default'} />
                </Box>
                <Typography variant="body2" color="text.secondary">
                  Tokeni se koriste za Super-Like i direktne poruke stanodavcima.
                  Neiskorišćeni tokeni ne ističu.
                </Typography>
              </CardContent>
              <CardActions>
                <Button size="small" onClick={() => navigate('/pricing')}>Kupi još</Button>
              </CardActions>
            </Card>
          </Grid>

          {/* Listing Credits */}
          <Grid item xs={12} sm={6}>
            <Card variant="outlined" sx={{ height: '100%' }}>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1.5 }}>
                  <HomeIcon color={status.listingCredits > 0 ? 'primary' : 'disabled'} />
                  <Typography fontWeight="bold">Listing krediti</Typography>
                  <Chip label={`${status.listingCredits} kredita`} size="small"
                    color={status.listingCredits > 0 ? 'primary' : 'default'} />
                </Box>
                <Typography variant="body2" color="text.secondary">
                  Svaki kredit vam omogućava objavljivanje jednog oglasa za nekretninu (30 dana).
                  Krediti ne ističu.
                </Typography>
              </CardContent>
              <CardActions>
                <Button size="small" onClick={() => navigate('/pricing')}>Kupi još</Button>
              </CardActions>
            </Card>
          </Grid>

          {/* Boost profila */}
          <Grid item xs={12} sm={6}>
            <Card variant="outlined" sx={{ height: '100%' }}>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1.5 }}>
                  <RocketLaunchIcon color={status.isBoosted ? 'primary' : 'disabled'} />
                  <Typography fontWeight="bold">Boost profila</Typography>
                  {status.isBoosted ? <ActiveChip /> : <InactiveChip />}
                </Box>
                {status.boostedUntil && status.isBoosted ? (
                  <Typography variant="body2" color="text.secondary">
                    Aktivan do: <strong>{formatDate(status.boostedUntil)}</strong>
                  </Typography>
                ) : (
                  <Typography variant="body2" color="text.secondary">
                    Vaš profil se pojavljuje u vrhu pretrage cimera dok je boost aktivan.
                  </Typography>
                )}
              </CardContent>
              <CardActions>
                <Button size="small" onClick={() => navigate('/pricing')}>
                  {status.isBoosted ? 'Produži' : 'Aktiviraj'}
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
                  <Typography fontWeight="bold">Priority Inbox</Typography>
                  {status.hasPriorityInbox ? <ActiveChip /> : <InactiveChip />}
                </Box>
                {status.priorityInboxUntil && status.hasPriorityInbox ? (
                  <Typography variant="body2" color="text.secondary">
                    Aktivan do: <strong>{formatDate(status.priorityInboxUntil)}</strong>
                  </Typography>
                ) : (
                  <Typography variant="body2" color="text.secondary">
                    Poruke od verifikovanih korisnika označene prioritetom.
                  </Typography>
                )}
              </CardContent>
              <CardActions>
                <Button size="small" onClick={() => navigate('/pricing')}>
                  {status.hasPriorityInbox ? 'Produži' : 'Aktiviraj'}
                </Button>
              </CardActions>
            </Card>
          </Grid>

        </Grid>
      )}

      <Divider sx={{ my: 4 }} />
      <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
        <Button variant="outlined" onClick={() => navigate('/istorija-placanja')}>
          Istorija plaćanja
        </Button>
        <Button variant="outlined" onClick={() => navigate('/pricing')}>
          Kupovina usluga
        </Button>
        <Button variant="text" href="/politika-povracaja" size="small" sx={{ ml: 'auto' }}>
          Politika povraćaja
        </Button>
      </Box>

      {/* Cancel analytics confirmation dialog */}
      <Dialog open={cancelOpen} onClose={() => setCancelOpen(false)} maxWidth="xs">
        <DialogTitle>Deaktivacija analitike</DialogTitle>
        <DialogContent>
          <Typography variant="body2">
            Da li ste sigurni da želite deaktivirati analitiku? Vaš nalog će biti vraćen na
            osnovni plan. Podaci o analitici biće izbrisani. <strong>Ova akcija je nepovratna i
            ne podrazumijeva povraćaj novca</strong> (usluga je već isporučena).
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCancelOpen(false)}>Odustani</Button>
          <Button color="error" onClick={cancelAnalytics} disabled={cancelling}>
            {cancelling ? <CircularProgress size={20} /> : 'Deaktiviraj'}
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
};

export default MySubscriptionsPage;
