import React, { useState } from 'react';
import {
  Alert, Avatar, Box, Button, Card, CardContent, Chip, CircularProgress, Container, Dialog, DialogActions,
  DialogContent, DialogContentText, DialogTitle, Divider, IconButton, Paper, Stack, Tooltip, Typography,
} from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import HandshakeIcon from '@mui/icons-material/Handshake';
import OpenIcon from '@mui/icons-material/OpenInNew';
import GroupsIcon from '@mui/icons-material/Groups';
import SearchIcon from '@mui/icons-material/Search';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMyPairing } from '../hooks/useMyPairing';
import { useNotifications } from '../shared/context/NotificationContext';
import { Pairing, roommatePairingApi, SharedApartment } from '../shared/api/roommatePairing';
import { chatGroupsApi } from '../shared/api/chatGroups';
import { SHARED_APARTMENTS_KEY } from '../shared/api/queryKeys';
import { getApiErrorMessage } from '../shared/utils/apiError';

const PairingRow: React.FC<{ pairing: Pairing; children?: React.ReactNode; caption: string }> = ({ pairing, children, caption }) => (
  <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, py: 1.5, flexWrap: 'wrap' }}>
    <Avatar src={pairing.partnerProfilePicture || undefined}>{pairing.partnerName?.[0]?.toUpperCase()}</Avatar>
    <Box sx={{ flex: 1, minWidth: 180 }}>
      <Typography fontWeight={600}>{pairing.partnerName}</Typography>
      <Typography variant="body2" color="text.secondary">{caption}</Typography>
    </Box>
    <Stack direction="row" spacing={1}>{children}</Stack>
  </Box>
);

const SharedApartmentRow: React.FC<{
  item: SharedApartment;
  onRemove: () => void;
  removing: boolean;
  onTalk: (landlordUserId: number) => void;
  talking: boolean;
}> = ({ item, onRemove, removing, onTalk, talking }) => {
  const { t } = useTranslation(['pairing']);
  const navigate = useNavigate();
  const amount = item.rent || item.price || 0;

  return (
    <Card variant="outlined" sx={{ borderRadius: 2, opacity: item.isAvailable ? 1 : 0.65 }}>
      <CardContent sx={{ display: 'flex', gap: 2, alignItems: 'flex-start', flexWrap: 'wrap', '&:last-child': { pb: 2 } }}>
        <Box sx={{ flex: 1, minWidth: 200 }}>
          <Typography fontWeight={700}>{item.title || t('listingUnavailable')}</Typography>
          <Typography variant="body2" color="text.secondary">
            {[item.address, item.city].filter(Boolean).join(', ')}
          </Typography>
          {item.isAvailable ? (
            <Typography variant="body2" sx={{ mt: 0.5 }} fontWeight={600}>{t('rentPerMonth', { amount })}</Typography>
          ) : (
            <Chip size="small" color="default" label={t('listingUnavailable')} sx={{ mt: 0.5 }} />
          )}
          {item.note && (
            <Typography variant="body2" sx={{ mt: 1, fontStyle: 'italic' }}>„{item.note}“</Typography>
          )}
          <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 0.5 }}>
            {t('addedBy', { name: item.addedByName })}
          </Typography>
          {item.isAvailable && item.landlordUserId && (
            <Button
              size="small" variant="outlined" startIcon={<GroupsIcon />} disabled={talking}
              onClick={() => onTalk(item.landlordUserId!)} sx={{ mt: 1, textTransform: 'none' }}
            >
              {item.landlordName ? t('talkToLandlordNamed', { name: item.landlordName }) : t('talkToLandlord')}
            </Button>
          )}
        </Box>
        <Stack direction="row" spacing={0.5}>
          {item.isAvailable && (
            <Tooltip title={t('openListing')} describeChild>
              <IconButton onClick={() => navigate(`/apartments/${item.apartmentId}`)} aria-label={t('openListing')}>
                <OpenIcon />
              </IconButton>
            </Tooltip>
          )}
          <Tooltip title={t('removeFromList')} describeChild>
            <span>
              <IconButton onClick={onRemove} disabled={removing} aria-label={t('removeFromList')}>
                <DeleteIcon />
              </IconButton>
            </span>
          </Tooltip>
        </Stack>
      </CardContent>
    </Card>
  );
};

const MyRoommatePage: React.FC = () => {
  const { t, i18n } = useTranslation(['pairing', 'validation', 'chat']);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { addNotification } = useNotifications();
  const { active, incoming, outgoing, isLoading, isError, refresh } = useMyPairing();
  const [confirmEnd, setConfirmEnd] = useState(false);

  const { data: shared = [], isLoading: sharedLoading } = useQuery({
    queryKey: [SHARED_APARTMENTS_KEY, active?.pairingId],
    queryFn: roommatePairingApi.getSharedApartments,
    enabled: !!active,
  });

  const afterChange = (title: string) => {
    refresh();
    queryClient.invalidateQueries({ queryKey: [SHARED_APARTMENTS_KEY] });
    queryClient.invalidateQueries({ queryKey: ['roommates'] });
    addNotification({ title, message: '', type: 'success' });
  };
  const onError = (error: unknown) =>
    addNotification({ title: t('pageTitle'), message: getApiErrorMessage(error, t), type: 'error' });

  const accept = useMutation({
    mutationFn: (id: number) => roommatePairingApi.accept(id),
    onSuccess: () => afterChange(t('pairingConfirmed')), onError,
  });
  const decline = useMutation({
    mutationFn: (id: number) => roommatePairingApi.decline(id),
    onSuccess: () => afterChange(t('proposalDeclined')), onError,
  });
  const withdraw = useMutation({
    mutationFn: (id: number) => roommatePairingApi.cancel(id),
    onSuccess: () => afterChange(t('proposalWithdrawn')), onError,
  });
  const end = useMutation({
    mutationFn: (id: number) => roommatePairingApi.end(id),
    onSuccess: () => { setConfirmEnd(false); afterChange(t('pairingEnded')); }, onError,
  });
  // A shared conversation with the landlord of a listed apartment: both roommates and the landlord in one chat.
  const talk = useMutation({
    mutationFn: (landlordUserId: number) => chatGroupsApi.open(landlordUserId),
    onSuccess: (group) => navigate(`/chat?group=${group.groupId}`),
    onError: (error) =>
      addNotification({ title: t('chat:groupOpenFailed'), message: getApiErrorMessage(error, t), type: 'error' }),
  });
  const remove = useMutation({
    mutationFn: (apartmentId: number) => roommatePairingApi.removeSharedApartment(apartmentId),
    onSuccess: () => afterChange(t('apartmentRemoved')), onError,
  });

  if (isLoading) {
    return <Container sx={{ py: 8, textAlign: 'center' }}><CircularProgress /></Container>;
  }

  const since = active
    ? new Date(active.respondedDate ?? active.createdDate).toLocaleDateString(i18n.language, { day: 'numeric', month: 'long', year: 'numeric' })
    : '';

  return (
    <Container maxWidth="md" sx={{ py: 4 }}>
      <Typography variant="h4" fontWeight={700} gutterBottom>{t('pageTitle')}</Typography>
      <Typography color="text.secondary" sx={{ mb: 3 }}>{t('pageSubtitle')}</Typography>

      {isError && <Alert severity="error" sx={{ mb: 2 }}>{t('errorLoading')}</Alert>}

      {incoming.length > 0 && (
        <Paper variant="outlined" sx={{ p: 2, mb: 3, borderRadius: 3, borderColor: 'primary.main' }}>
          <Typography variant="h6" fontWeight={700}>{t('incomingTitle')}</Typography>
          {incoming.map(p => (
            <PairingRow key={p.pairingId} pairing={p} caption={t('proposesToYou', { name: p.partnerName })}>
              <Button variant="contained" size="small" onClick={() => accept.mutate(p.pairingId)} disabled={accept.isPending || !!active}>
                {t('accept')}
              </Button>
              <Button size="small" onClick={() => decline.mutate(p.pairingId)} disabled={decline.isPending}>
                {t('decline')}
              </Button>
            </PairingRow>
          ))}
        </Paper>
      )}

      {active ? (
        <>
          <Paper sx={{ p: 3, mb: 3, borderRadius: 3 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, flexWrap: 'wrap' }}>
              <Avatar src={active.partnerProfilePicture || undefined} sx={{ width: 64, height: 64 }}>
                {active.partnerName?.[0]?.toUpperCase()}
              </Avatar>
              <Box sx={{ flex: 1, minWidth: 180 }}>
                <Typography variant="overline" color="text.secondary">{t('activeTitle')}</Typography>
                <Typography variant="h5" fontWeight={700}>{active.partnerName}</Typography>
                <Typography variant="body2" color="text.secondary">{t('roommatesSince', { date: since })}</Typography>
              </Box>
              <Chip color="success" icon={<HandshakeIcon />} label={t('taken')} />
            </Box>
            <Divider sx={{ my: 2 }} />
            <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
              <Button variant="outlined" onClick={() => navigate(`/chat?userId=${active.partnerUserId}`)}>
                {t('chat:startConversation')}
              </Button>
              <Button color="error" onClick={() => setConfirmEnd(true)}>{t('endPairing')}</Button>
            </Stack>
          </Paper>

          <Paper sx={{ p: 3, borderRadius: 3 }}>
            <Typography variant="h6" fontWeight={700}>{t('sharedTitle')}</Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>{t('sharedHint')}</Typography>
            {sharedLoading ? (
              <CircularProgress size={24} />
            ) : shared.length === 0 ? (
              <Alert severity="info" icon={false}>{t('sharedEmpty')}</Alert>
            ) : (
              <Stack spacing={1.5}>
                {shared.map(item => (
                  <SharedApartmentRow
                    key={item.apartmentId}
                    item={item}
                    onRemove={() => remove.mutate(item.apartmentId)}
                    removing={remove.isPending}
                    onTalk={(landlordUserId) => talk.mutate(landlordUserId)}
                    talking={talk.isPending}
                  />
                ))}
              </Stack>
            )}
          </Paper>
        </>
      ) : (
        <Paper sx={{ p: 4, textAlign: 'center', borderRadius: 3 }}>
          <HandshakeIcon sx={{ fontSize: 48, color: 'text.secondary', mb: 1 }} />
          <Typography variant="h6" fontWeight={700}>{t('noRoommateTitle')}</Typography>
          <Typography color="text.secondary" sx={{ maxWidth: 520, mx: 'auto', my: 1.5 }}>{t('noRoommateHint')}</Typography>
          <Button variant="contained" startIcon={<SearchIcon />} onClick={() => navigate('/roommates')}>
            {t('findRoommate')}
          </Button>
        </Paper>
      )}

      {outgoing.length > 0 && !active && (
        <Paper variant="outlined" sx={{ p: 2, mt: 3, borderRadius: 3 }}>
          <Typography variant="h6" fontWeight={700}>{t('outgoingTitle')}</Typography>
          {outgoing.map(p => (
            <PairingRow key={p.pairingId} pairing={p} caption={t('waitingFor', { name: p.partnerName })}>
              <Button size="small" onClick={() => withdraw.mutate(p.pairingId)} disabled={withdraw.isPending}>
                {t('withdraw')}
              </Button>
            </PairingRow>
          ))}
        </Paper>
      )}

      <Dialog open={confirmEnd} onClose={() => setConfirmEnd(false)}>
        <DialogTitle>{t('endConfirmTitle')}</DialogTitle>
        <DialogContent><DialogContentText>{t('endConfirmText')}</DialogContentText></DialogContent>
        <DialogActions>
          <Button onClick={() => setConfirmEnd(false)}>{t('keepPairing')}</Button>
          <Button color="error" variant="contained" disabled={end.isPending || !active}
            onClick={() => active && end.mutate(active.pairingId)}>
            {t('endPairing')}
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
};

export default MyRoommatePage;
