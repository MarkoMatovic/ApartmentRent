import React, { useState } from 'react';
import {
  Alert, Avatar, Box, Button, Chip, CircularProgress, Container, Dialog, DialogActions, DialogContent,
  DialogContentText, DialogTitle, List, ListItem, ListItemAvatar, ListItemButton, ListItemText, Paper, Stack,
  Typography,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import BackIcon from '@mui/icons-material/ArrowBack';
import { useInfiniteQuery, useQuery } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Bill, billsApi, formatDay, formatMoney, TenancyMember } from '../shared/api/bills';
import { BILLS_KEY, TENANCIES_KEY } from '../shared/api/queryKeys';
import { useAuth } from '../shared/context/AuthContext';
import BillCard from '../components/Bills/BillCard';
import BillFormDialog from '../components/Bills/BillFormDialog';
import TenancyFormDialog from '../components/Bills/TenancyFormDialog';
import { useBillActions } from '../components/Bills/useBillActions';

const PAGE_SIZE = 20;

/** One tenancy: who lives there, the rent settings, and every bill with its payments. */
const TenancyPage: React.FC = () => {
  const { id } = useParams();
  const tenancyId = Number(id);
  const { t, i18n } = useTranslation(['bills', 'common']);
  const navigate = useNavigate();
  const { user } = useAuth();
  const me = user?.userId ?? 0;
  const { run, runInDialog } = useBillActions();
  const [billForm, setBillForm] = useState<{ open: boolean; bill: Bill | null }>({ open: false, bill: null });
  const [editingSettings, setEditingSettings] = useState(false);
  const [addingTenant, setAddingTenant] = useState(false);
  const [confirm, setConfirm] = useState<{ text: string; action: () => Promise<unknown> } | null>(null);

  const tenancyQuery = useQuery({
    queryKey: [TENANCIES_KEY, tenancyId],
    queryFn: () => billsApi.getTenancy(tenancyId),
    enabled: Number.isFinite(tenancyId),
    retry: false,
  });
  const billsQuery = useInfiniteQuery({
    queryKey: [BILLS_KEY, 'tenancy', tenancyId],
    queryFn: ({ pageParam }) => billsApi.getTenancyBills(tenancyId, pageParam, PAGE_SIZE),
    initialPageParam: 1,
    getNextPageParam: last => (last.page * last.pageSize < last.totalCount ? last.page + 1 : undefined),
    enabled: tenancyQuery.isSuccess,
  });
  const tenancy = tenancyQuery.data;
  const candidatesQuery = useQuery({
    queryKey: ['tenancy-candidates', tenancy?.apartmentId],
    queryFn: () => billsApi.getCandidates(tenancy!.apartmentId),
    enabled: addingTenant && !!tenancy,
  });

  if (tenancyQuery.isLoading) {
    return <Box sx={{ textAlign: 'center', py: 8 }}><CircularProgress /></Box>;
  }
  if (!tenancy) {
    return (
      <Container maxWidth="md" sx={{ py: 4 }}>
        <Alert severity="warning">{t('bills:notFound')}</Alert>
        <Button startIcon={<BackIcon />} sx={{ mt: 2 }} onClick={() => navigate('/bills')}>{t('bills:backToBills')}</Button>
      </Container>
    );
  }

  const active = tenancy.status === 'active';
  const current = tenancy.members.filter(m => !m.leftDate);
  const iAmTenant = current.some(m => m.userId === me);
  const bills = billsQuery.data?.pages.flatMap(p => p.bills) ?? [];
  const money = (amount: number) => formatMoney(amount, i18n.language);

  const memberLine = (m: TenancyMember) =>
    m.leftDate
      ? t('bills:leftOn', { date: formatDay(m.leftDate, i18n.language) })
      : t('bills:since', { date: formatDay(m.joinedDate, i18n.language) });

  return (
    <Container maxWidth="md" sx={{ py: 4 }}>
      <Button startIcon={<BackIcon />} onClick={() => navigate('/bills')} sx={{ mb: 2 }}>{t('bills:backToBills')}</Button>

      <Paper variant="outlined" sx={{ p: 3, borderRadius: 3, mb: 3 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 2, flexWrap: 'wrap' }}>
          <Box>
            <Typography variant="h5" component="h1" fontWeight={700}>{tenancy.apartmentTitle}</Typography>
            <Typography color="text.secondary">
              {tenancy.isLandlord ? t('bills:yourApartment') : t('bills:landlordLine', { name: tenancy.landlord.name })}
            </Typography>
          </Box>
          {!active && <Chip label={t('bills:ended')} />}
        </Box>
        <Typography sx={{ mt: 2 }} fontWeight={600}>
          {t('bills:rentLine', { amount: money(tenancy.monthlyRent), day: tenancy.rentDueDay })}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {t('bills:payeeLine', { name: tenancy.payeeName, account: tenancy.payeeAccount })}
        </Typography>

        <Stack direction="row" spacing={1} sx={{ mt: 2, flexWrap: 'wrap', gap: 1 }}>
          {active && (tenancy.isLandlord || iAmTenant) && (
            <Button variant="contained" startIcon={<AddIcon />} onClick={() => setBillForm({ open: true, bill: null })}>
              {t('bills:addBill')}
            </Button>
          )}
          {active && tenancy.isLandlord && (
            <>
              <Button onClick={() => setEditingSettings(true)}>{t('bills:settings')}</Button>
              <Button onClick={() => setAddingTenant(true)}>{t('bills:addTenant')}</Button>
              <Button color="error" onClick={() => setConfirm({ text: t('bills:endConfirm'), action: () => billsApi.endTenancy(tenancyId) })}>
                {t('bills:endTenancy')}
              </Button>
            </>
          )}
          {active && iAmTenant && (
            <Button color="error" onClick={() => setConfirm({
              text: t('bills:leaveConfirm'),
              action: async () => { await billsApi.removeTenant(tenancyId, me); navigate('/bills'); },
            })}>
              {t('bills:leave')}
            </Button>
          )}
        </Stack>

        <Typography variant="subtitle1" fontWeight={700} sx={{ mt: 3 }}>{t('bills:tenants')}</Typography>
        <List dense>
          {tenancy.members.map(m => (
            <ListItem
              key={`${m.userId}-${m.joinedDate}`}
              disableGutters
              sx={{ opacity: m.leftDate ? 0.6 : 1 }}
              secondaryAction={active && tenancy.isLandlord && !m.leftDate ? (
                <Button size="small" color="error" onClick={() => setConfirm({
                  text: t('bills:removeTenantConfirm', { name: m.name }),
                  action: () => billsApi.removeTenant(tenancyId, m.userId),
                })}>
                  {t('bills:removeTenant')}
                </Button>
              ) : undefined}
            >
              <ListItemAvatar><Avatar src={m.profilePicture || undefined}>{m.name[0]}</Avatar></ListItemAvatar>
              <ListItemText primary={m.name} secondary={memberLine(m)} />
            </ListItem>
          ))}
        </List>
      </Paper>

      <Typography variant="h6" fontWeight={700} sx={{ mb: 1.5 }}>{t('bills:history')}</Typography>
      {billsQuery.isLoading ? (
        <Box sx={{ textAlign: 'center', py: 2 }}><CircularProgress size={28} /></Box>
      ) : bills.length === 0 ? (
        <Typography color="text.secondary">{t('bills:noBills')}</Typography>
      ) : (
        <Stack spacing={1.5}>
          {bills.map(bill => (
            <BillCard key={bill.billId} bill={bill} currentUserId={me} onEdit={b => setBillForm({ open: true, bill: b })} />
          ))}
          {billsQuery.hasNextPage && (
            <Button onClick={() => billsQuery.fetchNextPage()} disabled={billsQuery.isFetchingNextPage}>{t('bills:loadMore')}</Button>
          )}
        </Stack>
      )}

      <BillFormDialog
        open={billForm.open}
        tenancyId={tenancyId}
        isLandlord={tenancy.isLandlord}
        tenantCount={current.length}
        bill={billForm.bill}
        onClose={() => setBillForm({ open: false, bill: null })}
        onCreate={input => runInDialog(() => billsApi.createBill(input))}
        onUpdate={(billId, input) => runInDialog(() => billsApi.updateBill(billId, input))}
      />
      <TenancyFormDialog
        open={editingSettings}
        tenancy={tenancy}
        onClose={() => setEditingSettings(false)}
        onCreate={async () => undefined}
        onUpdate={(id, input) => runInDialog(() => billsApi.updateTenancy(id, input))}
      />

      <Dialog open={addingTenant} onClose={() => setAddingTenant(false)} maxWidth="xs" fullWidth>
        <DialogTitle>{t('bills:addTenant')}</DialogTitle>
        <DialogContent>
          {candidatesQuery.isLoading ? (
            <CircularProgress size={24} />
          ) : (candidatesQuery.data ?? []).length === 0 ? (
            <Alert severity="info">{t('bills:noCandidates')}</Alert>
          ) : (
            <List>
              {(candidatesQuery.data ?? []).map(c => (
                <ListItemButton key={c.userId} onClick={() => { setAddingTenant(false); run(() => billsApi.addTenant(tenancyId, c.userId)); }}>
                  <ListItemAvatar><Avatar src={c.profilePicture || undefined}>{c.name[0]}</Avatar></ListItemAvatar>
                  <ListItemText
                    primary={c.name}
                    secondary={c.reason === 'roommate' ? t('bills:candidateRoommate') : t('bills:candidateApplication')}
                  />
                </ListItemButton>
              ))}
            </List>
          )}
        </DialogContent>
        <DialogActions><Button onClick={() => setAddingTenant(false)}>{t('common:close')}</Button></DialogActions>
      </Dialog>

      <Dialog open={confirm !== null} onClose={() => setConfirm(null)}>
        <DialogContent><DialogContentText>{confirm?.text}</DialogContentText></DialogContent>
        <DialogActions>
          <Button onClick={() => setConfirm(null)}>{t('common:cancel')}</Button>
          <Button color="error" onClick={() => { const action = confirm!.action; setConfirm(null); run(action); }}>
            {t('bills:confirm')}
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
};

export default TenancyPage;
