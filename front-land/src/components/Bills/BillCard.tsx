import React, { useState } from 'react';
import {
  Avatar, Box, Button, Card, CardContent, Collapse, Dialog, DialogActions, DialogContent, DialogContentText,
  Divider, Stack, Typography,
} from '@mui/material';
import { useTranslation } from 'react-i18next';
import { Bill, BillShare, billsApi, formatDay, formatMoney } from '../../shared/api/bills';
import PaymentDialog from './PaymentDialog';
import PayCompanyDialog from './PayCompanyDialog';
import { BillStatusChip, billTitle, ShareStatusChip } from './billUi';
import { useBillActions } from './useBillActions';

interface Props {
  bill: Bill;
  currentUserId: number;
  onEdit?: (bill: Bill) => void;
}

/** One bill with who pays what, and the steps the signed-in user can take on it. */
const BillCard: React.FC<Props> = ({ bill, currentUserId, onEdit }) => {
  const { t, i18n } = useTranslation(['bills', 'common']);
  const { run, runInDialog } = useBillActions();
  const [payingShare, setPayingShare] = useState<BillShare | null>(null);
  const [payingCompany, setPayingCompany] = useState(false);
  const [confirmCancel, setConfirmCancel] = useState(false);
  const [expanded, setExpanded] = useState(bill.shares.some(s => s.payer.userId === currentUserId || s.receiver.userId === currentUserId));
  const title = billTitle(t, bill, i18n.language);
  const money = (amount: number) => `${formatMoney(amount, i18n.language)} RSD`;

  return (
    <Card variant="outlined" sx={{ borderRadius: 2, opacity: bill.status === 'cancelled' ? 0.6 : 1 }} data-testid={`bill-${bill.billId}`}>
      <CardContent sx={{ '&:last-child': { pb: 2 } }}>
        <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap', alignItems: 'flex-start' }}>
          <Box sx={{ flex: 1, minWidth: 200 }}>
            <Typography variant="overline" color="text.secondary">{t(`bills:kind_${bill.kind}`)}</Typography>
            <Typography fontWeight={700}>{title}</Typography>
            <Typography variant="body2" color="text.secondary">
              {t('bills:due', { date: formatDay(bill.dueDate, i18n.language) })} · {t('bills:receiverLine', { name: bill.payeeName })}
            </Typography>
            {bill.paidBy && (
              <Typography variant="body2" color="text.secondary">{t('bills:paidByLine', { name: bill.paidBy.name })}</Typography>
            )}
          </Box>
          <Box sx={{ textAlign: 'right' }}>
            <Typography variant="h6" fontWeight={700}>{money(bill.amount)}</Typography>
            <BillStatusChip bill={bill} t={t} />
          </Box>
        </Box>

        <Stack direction="row" spacing={1} sx={{ mt: 1.5, flexWrap: 'wrap', gap: 1 }}>
          {bill.canPayCompany && (
            <Button size="small" variant="contained" onClick={() => setPayingCompany(true)}>{t('bills:payWhole')}</Button>
          )}
          {bill.canUndoCompanyPayment && (
            <Button size="small" onClick={() => run(() => billsApi.undoCompanyPayment(bill.billId))}>{t('bills:undoCompanyPayment')}</Button>
          )}
          {bill.canEdit && onEdit && <Button size="small" onClick={() => onEdit(bill)}>{t('bills:editBill')}</Button>}
          {bill.canEdit && <Button size="small" color="error" onClick={() => setConfirmCancel(true)}>{t('bills:cancelBill')}</Button>}
          {bill.shares.length > 0 && (
            <Button size="small" onClick={() => setExpanded(v => !v)} aria-expanded={expanded}>{t('bills:sharesTitle')}</Button>
          )}
        </Stack>

        <Collapse in={expanded}>
          {bill.shares.length > 0 && <Divider sx={{ my: 1.5 }} />}
          <Stack spacing={1}>
            {bill.shares.map(share => {
              const iPay = share.payer.userId === currentUserId;
              return (
                <Box key={share.billShareId} sx={{ display: 'flex', alignItems: 'center', gap: 1.5, flexWrap: 'wrap' }} data-testid={`share-${share.billShareId}`}>
                  <Avatar src={share.payer.profilePicture || undefined} sx={{ width: 30, height: 30 }}>{share.payer.name[0]}</Avatar>
                  <Box sx={{ flex: 1, minWidth: 160 }}>
                    <Typography variant="body2" fontWeight={600}>{share.payer.name} → {share.receiver.name}</Typography>
                    <Typography variant="body2" color="text.secondary">{money(share.amount)}</Typography>
                  </Box>
                  <ShareStatusChip status={share.status} t={t} />
                  {share.canMarkPaid && (
                    <Button size="small" variant="contained" onClick={() => setPayingShare(share)}>{t('bills:pay')}</Button>
                  )}
                  {iPay && share.status === 'marked_paid' && (
                    <Button size="small" onClick={() => run(() => billsApi.unmarkShare(share.billShareId))}>{t('bills:undoPaid')}</Button>
                  )}
                  {share.canConfirm && (
                    <Button size="small" color="success" variant="outlined" onClick={() => run(() => billsApi.confirmShare(share.billShareId))}>
                      {t('bills:received')}
                    </Button>
                  )}
                  {share.canConfirm && share.status === 'marked_paid' && (
                    <Button size="small" color="warning" onClick={() => run(() => billsApi.rejectShare(share.billShareId))}>
                      {t('bills:notReceived')}
                    </Button>
                  )}
                </Box>
              );
            })}
          </Stack>
        </Collapse>
      </CardContent>

      <PaymentDialog
        open={payingShare !== null}
        title={title}
        load={() => billsApi.getSharePayment(payingShare!.billShareId)}
        onClose={() => setPayingShare(null)}
        confirmLabel={t('bills:iPaid')}
        onConfirm={() => runInDialog(() => billsApi.markSharePaid(payingShare!.billShareId))}
      />
      <PayCompanyDialog
        bill={payingCompany ? bill : null}
        onClose={() => setPayingCompany(false)}
        onPaid={payout => runInDialog(() => billsApi.payCompanyBill(bill.billId, payout))}
      />
      <Dialog open={confirmCancel} onClose={() => setConfirmCancel(false)}>
        <DialogContent>
          <DialogContentText>{t('bills:cancelBillConfirm', { title })}</DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setConfirmCancel(false)}>{t('common:cancel')}</Button>
          <Button color="error" onClick={() => { setConfirmCancel(false); run(() => billsApi.cancelBill(bill.billId)); }}>
            {t('bills:cancelBill')}
          </Button>
        </DialogActions>
      </Dialog>
    </Card>
  );
};

export default BillCard;
