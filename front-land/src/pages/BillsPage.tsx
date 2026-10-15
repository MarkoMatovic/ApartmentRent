import React, { useRef, useState } from 'react';
import {
  Alert, Avatar, Box, Button, Card, CardActionArea, CardContent, Chip, CircularProgress, Container, Grid, Paper,
  Stack, Typography,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import ReceiptIcon from '@mui/icons-material/ReceiptLong';
import { useQuery } from '@tanstack/react-query';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { billsApi, formatDay, formatMoney, PaymentDue } from '../shared/api/bills';
import { BILLS_KEY, TENANCIES_KEY } from '../shared/api/queryKeys';
import { useAuth } from '../shared/context/AuthContext';
import PaymentDialog from '../components/Bills/PaymentDialog';
import PayCompanyDialog from '../components/Bills/PayCompanyDialog';
import TenancyFormDialog from '../components/Bills/TenancyFormDialog';
import { BillStatusChip, billTitle } from '../components/Bills/billUi';
import { useBillActions } from '../components/Bills/useBillActions';

/**
 * Bills of the signed-in user: what they have to pay, payments to them waiting for "arrived", and
 * their tenancies (as a landlord or a tenant).
 */
const BillsPage: React.FC = () => {
  const { t, i18n } = useTranslation(['bills', 'common']);
  const navigate = useNavigate();
  const { user } = useAuth();
  const { run, runInDialog } = useBillActions();
  const [paying, setPaying] = useState<PaymentDue | null>(null);
  // "Create the tenancy" on an approved application opens the form with that apartment chosen.
  const [searchParams, setSearchParams] = useSearchParams();
  const linkedApartment = Number(searchParams.get('newTenancy')) || undefined;
  const [creatingTenancy, setCreatingTenancy] = useState(linkedApartment !== undefined);
  // Set once the new tenancy exists: its page is opened, so closing the form must not change the address again.
  const openedTenancyRef = useRef(false);
  const money = (amount: number) => `${formatMoney(amount, i18n.language)} RSD`;

  const mine = useQuery({ queryKey: [BILLS_KEY, 'mine'], queryFn: billsApi.getMine });
  const tenancies = useQuery({ queryKey: [TENANCIES_KEY], queryFn: billsApi.getTenancies });

  const toPay = mine.data?.toPay ?? [];
  const toConfirm = mine.data?.toConfirm ?? [];
  const totalUnpaid = toPay.filter(p => p.status === 'unpaid').reduce((sum, p) => sum + p.amount, 0);
  const defaultPayeeName = user ? `${user.firstName} ${user.lastName}`.trim() : '';

  return (
    <Container maxWidth="md" sx={{ py: 4 }}>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 1 }}>
        <ReceiptIcon color="primary" />
        <Typography variant="h4" component="h1" fontWeight={700}>{t('bills:title')}</Typography>
      </Box>
      <Typography color="text.secondary" sx={{ mb: 3 }}>{t('bills:subtitle')}</Typography>

      {(mine.isError || tenancies.isError) && <Alert severity="error" sx={{ mb: 2 }}>{t('bills:loadFailed')}</Alert>}

      {/* What I have to pay */}
      <Paper variant="outlined" sx={{ p: 2.5, borderRadius: 3, mb: 3 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 1, mb: 1 }}>
          <Typography variant="h6" fontWeight={700}>{t('bills:toPay')}</Typography>
          {totalUnpaid > 0 && <Chip color="primary" label={t('bills:totalToPay', { amount: formatMoney(totalUnpaid, i18n.language) })} />}
        </Box>
        {mine.isLoading ? (
          <Box sx={{ textAlign: 'center', py: 2 }}><CircularProgress size={28} /></Box>
        ) : toPay.length === 0 ? (
          <Typography color="text.secondary">{t('bills:toPayEmpty')}</Typography>
        ) : (
          <Stack spacing={1.5}>
            {toPay.map(item => (
              <Card key={`${item.type}-${item.billShareId ?? item.bill.billId}`} variant="outlined" sx={{ borderRadius: 2 }}
                data-testid="to-pay-item">
                <CardContent sx={{ display: 'flex', gap: 2, alignItems: 'center', flexWrap: 'wrap', '&:last-child': { pb: 2 } }}>
                  <Box sx={{ flex: 1, minWidth: 200 }}>
                    <Typography fontWeight={700}>{billTitle(t, item.bill, i18n.language)}</Typography>
                    <Typography variant="body2" color="text.secondary">{item.bill.apartmentTitle}</Typography>
                    <Typography variant="body2" color="text.secondary">
                      {t('bills:due', { date: formatDay(item.bill.dueDate, i18n.language) })}
                      {' · '}
                      {t('bills:receiverLine', { name: item.receiver?.name ?? item.bill.payeeName })}
                    </Typography>
                    {item.status === 'marked_paid' && (
                      <Typography variant="body2" color="info.main" sx={{ mt: 0.5 }}>{t('bills:markedPaidWaiting')}</Typography>
                    )}
                  </Box>
                  <Box sx={{ textAlign: 'right' }}>
                    <Typography variant="h6" fontWeight={700}>{money(item.amount)}</Typography>
                    {item.bill.isOverdue && <BillStatusChip bill={item.bill} t={t} />}
                  </Box>
                  <Stack direction="row" spacing={1}>
                    {item.status === 'marked_paid' && item.billShareId ? (
                      <Button size="small" onClick={() => run(() => billsApi.unmarkShare(item.billShareId!))}>{t('bills:undoPaid')}</Button>
                    ) : (
                      <Button variant="contained" onClick={() => setPaying(item)}>
                        {item.type === 'company_bill' ? t('bills:payWhole') : t('bills:pay')}
                      </Button>
                    )}
                  </Stack>
                </CardContent>
              </Card>
            ))}
          </Stack>
        )}
      </Paper>

      {/* Payments to me that wait for "arrived" */}
      {toConfirm.length > 0 && (
        <Paper variant="outlined" sx={{ p: 2.5, borderRadius: 3, mb: 3 }}>
          <Typography variant="h6" fontWeight={700}>{t('bills:toConfirm')}</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>{t('bills:toConfirmHint')}</Typography>
          <Stack spacing={1.5}>
            {toConfirm.map(({ share, bill }) => (
              <Box key={share.billShareId} sx={{ display: 'flex', alignItems: 'center', gap: 1.5, flexWrap: 'wrap' }} data-testid="to-confirm-item">
                <Avatar src={share.payer.profilePicture || undefined}>{share.payer.name[0]}</Avatar>
                <Box sx={{ flex: 1, minWidth: 180 }}>
                  <Typography fontWeight={600}>{share.payer.name} · {money(share.amount)}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    {billTitle(t, bill, i18n.language)} · {bill.apartmentTitle}
                  </Typography>
                </Box>
                <Button size="small" variant="contained" color="success" onClick={() => run(() => billsApi.confirmShare(share.billShareId))}>
                  {t('bills:received')}
                </Button>
                <Button size="small" color="warning" onClick={() => run(() => billsApi.rejectShare(share.billShareId))}>
                  {t('bills:notReceived')}
                </Button>
              </Box>
            ))}
          </Stack>
        </Paper>
      )}

      {/* Tenancies */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1.5, flexWrap: 'wrap', gap: 1 }}>
        <Typography variant="h6" fontWeight={700}>{t('bills:tenancies')}</Typography>
        <Button startIcon={<AddIcon />} onClick={() => setCreatingTenancy(true)}>{t('bills:newTenancy')}</Button>
      </Box>
      {tenancies.isLoading ? (
        <Box sx={{ textAlign: 'center', py: 2 }}><CircularProgress size={28} /></Box>
      ) : (tenancies.data ?? []).length === 0 ? (
        <Typography color="text.secondary">{t('bills:tenanciesEmpty')}</Typography>
      ) : (
        <Grid container spacing={2}>
          {(tenancies.data ?? []).map(tenancy => (
            <Grid item xs={12} sm={6} key={tenancy.tenancyId}>
              <Card variant="outlined" sx={{ borderRadius: 2, height: '100%' }}>
                <CardActionArea onClick={() => navigate(`/bills/tenancy/${tenancy.tenancyId}`)} sx={{ height: '100%' }}>
                  <CardContent>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 1 }}>
                      <Typography fontWeight={700}>{tenancy.apartmentTitle}</Typography>
                      {tenancy.status === 'ended' && <Chip size="small" label={t('bills:ended')} />}
                    </Box>
                    <Typography variant="body2" color="text.secondary">
                      {tenancy.isLandlord ? t('bills:yourApartment') : t('bills:landlordLine', { name: tenancy.landlord.name })}
                    </Typography>
                    <Typography variant="body2" sx={{ mt: 1 }}>
                      {t('bills:rentLine', { amount: formatMoney(tenancy.monthlyRent, i18n.language), day: tenancy.rentDueDay })}
                    </Typography>
                    {tenancy.openBills > 0 && (
                      <Chip size="small" color="warning" sx={{ mt: 1 }} label={t('bills:openBills', { count: tenancy.openBills })} />
                    )}
                  </CardContent>
                </CardActionArea>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}

      {paying?.type === 'share' && paying.billShareId && (
        <PaymentDialog
          open
          title={billTitle(t, paying.bill, i18n.language)}
          load={() => billsApi.getSharePayment(paying.billShareId!)}
          onClose={() => setPaying(null)}
          confirmLabel={t('bills:iPaid')}
          onConfirm={() => runInDialog(() => billsApi.markSharePaid(paying.billShareId!))}
        />
      )}
      <PayCompanyDialog
        bill={paying?.type === 'company_bill' ? paying.bill : null}
        onClose={() => setPaying(null)}
        onPaid={payout => runInDialog(() => billsApi.payCompanyBill(paying!.bill.billId, payout))}
      />
      <TenancyFormDialog
        open={creatingTenancy}
        initialApartmentId={linkedApartment}
        defaultPayeeName={defaultPayeeName}
        onClose={() => {
          setCreatingTenancy(false);
          if (linkedApartment !== undefined && !openedTenancyRef.current) setSearchParams({}, { replace: true });
        }}
        onCreate={async input => {
          let created = 0;
          await runInDialog(async () => { created = (await billsApi.createTenancy(input)).tenancyId; });
          openedTenancyRef.current = true;
          navigate(`/bills/tenancy/${created}`);
        }}
        onUpdate={async () => undefined}
      />
    </Container>
  );
};

export default BillsPage;
