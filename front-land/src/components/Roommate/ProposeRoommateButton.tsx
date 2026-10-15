import React from 'react';
import { Alert, Box, Button, Chip } from '@mui/material';
import HandshakeIcon from '@mui/icons-material/Handshake';
import { useMutation } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../../shared/context/AuthContext';
import { useNotifications } from '../../shared/context/NotificationContext';
import { useMyPairing } from '../../hooks/useMyPairing';
import { roommatePairingApi } from '../../shared/api/roommatePairing';
import { getApiErrorMessage } from '../../shared/utils/apiError';

interface Props {
  /** The person whose profile is being looked at. */
  userId: number;
  firstName: string;
  /** They already agreed to be roommates with someone else. */
  isTaken?: boolean;
  /** A small button for a list card: says nothing where the card already says it (taken, my roommate). */
  compact?: boolean;
}

/**
 * "Propose to be roommates" on somebody's profile. It works out from the signed-in user's own
 * situation what makes sense: nothing for a taken profile, "accept" when they already proposed to
 * me, a waiting note when I proposed to them.
 */
const ProposeRoommateButton: React.FC<Props> = ({ userId, firstName, isTaken, compact }) => {
  const { t } = useTranslation(['pairing', 'validation']);
  const { user } = useAuth();
  const { addNotification } = useNotifications();
  const { active, incoming, outgoing, refresh } = useMyPairing();

  const propose = useMutation({
    mutationFn: () => roommatePairingApi.propose(userId),
    onSuccess: (pairing) => {
      refresh();
      addNotification(pairing.status === 'Accepted'
        ? { title: t('pairingConfirmed'), message: t('proposalSentHint', { name: firstName }), type: 'success' }
        : { title: t('proposalSent'), message: t('proposalSentHint', { name: firstName }), type: 'success' });
    },
    onError: (error) => addNotification({ title: t('proposeButton'), message: getApiErrorMessage(error, t), type: 'error' }),
  });

  const accept = useMutation({
    mutationFn: (pairingId: number) => roommatePairingApi.accept(pairingId),
    onSuccess: () => {
      refresh();
      addNotification({ title: t('pairingConfirmed'), message: '', type: 'success' });
    },
    onError: (error) => addNotification({ title: t('accept'), message: getApiErrorMessage(error, t), type: 'error' }),
  });

  if (!user || user.userId === userId) return null;

  if (compact) {
    // Nothing to offer once somebody is taken, or when this is my own roommate; the card says so itself.
    if (active || isTaken) return null;

    const theirs = incoming.find(p => p.partnerUserId === userId);
    if (theirs) {
      return (
        <Button size="small" color="success" variant="contained" startIcon={<HandshakeIcon />}
          onClick={() => accept.mutate(theirs.pairingId)} disabled={accept.isPending}>
          {t('acceptProposal')}
        </Button>
      );
    }
    const sent = outgoing.some(p => p.partnerUserId === userId);
    return (
      <Button size="small" variant="outlined" startIcon={<HandshakeIcon />}
        onClick={() => propose.mutate()} disabled={sent || propose.isPending}>
        {sent ? t('proposalSent') : t('proposeButton')}
      </Button>
    );
  }

  if (active?.partnerUserId === userId) {
    return <Chip color="success" label={t('activeTitle')} sx={{ mb: 1.5, fontWeight: 600 }} />;
  }
  if (active) {
    return <Alert severity="info" sx={{ mb: 1.5 }}>{t('youAreTaken')}</Alert>;
  }
  if (isTaken) {
    return <Alert severity="warning" sx={{ mb: 1.5 }}>{t('takenHint')}</Alert>;
  }

  const theirProposal = incoming.find(p => p.partnerUserId === userId);
  if (theirProposal) {
    return (
      <Alert
        severity="info"
        sx={{ mb: 1.5 }}
        action={
          <Button color="inherit" size="small" onClick={() => accept.mutate(theirProposal.pairingId)} disabled={accept.isPending}>
            {t('accept')}
          </Button>
        }
      >
        {t('proposesToYou', { name: firstName })}
      </Alert>
    );
  }

  const mine = outgoing.some(p => p.partnerUserId === userId);
  return (
    <Box>
      <Button
        fullWidth variant="outlined" size="large" startIcon={<HandshakeIcon />}
        onClick={() => propose.mutate()} disabled={mine || propose.isPending}
        sx={{ mb: 1.5, borderRadius: 2 }}
      >
        {mine ? t('waitingFor', { name: firstName }) : t('proposeButton')}
      </Button>
    </Box>
  );
};

export default ProposeRoommateButton;
