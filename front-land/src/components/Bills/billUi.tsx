import React from 'react';
import { Chip } from '@mui/material';
import type { TFunction } from 'i18next';
import { Bill, BillStatus, intlLocale, ShareStatus } from '../../shared/api/bills';

/** A rent bill is named after its month in the reader's language ("Kirija za oktobar 2026."); others keep their name. */
export function billTitle(t: TFunction, bill: Pick<Bill, 'kind' | 'period' | 'title'>, language: string): string {
  if (bill.kind === 'rent' && bill.period && /^\d{4}-\d{2}$/.test(bill.period)) {
    const [year, month] = bill.period.split('-').map(Number);
    const period = new Intl.DateTimeFormat(intlLocale(language), { month: 'long', year: 'numeric' })
      .format(new Date(year, month - 1, 1));
    return t('bills:rentFor', { period });
  }
  return bill.title;
}

const billColor: Record<BillStatus, 'default' | 'success' | 'warning' | 'info'> = {
  open: 'warning',
  partly_paid: 'info',
  paid: 'success',
  cancelled: 'default',
};

export const BillStatusChip: React.FC<{ bill: Pick<Bill, 'status' | 'isOverdue'>; t: TFunction }> = ({ bill, t }) =>
  bill.isOverdue ? (
    <Chip size="small" color="error" label={t('bills:overdue')} />
  ) : (
    <Chip size="small" color={billColor[bill.status]} label={t(`bills:status_${bill.status}`)} />
  );

const shareColor: Record<ShareStatus, 'default' | 'success' | 'info'> = {
  unpaid: 'default',
  marked_paid: 'info',
  confirmed: 'success',
};

export const ShareStatusChip: React.FC<{ status: ShareStatus; t: TFunction }> = ({ status, t }) => (
  <Chip size="small" variant="outlined" color={shareColor[status]} label={t(`bills:share_${status}`)} />
);
