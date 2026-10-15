import { useQueryClient } from '@tanstack/react-query';
import { useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { BILLS_KEY, BILLS_SUMMARY_KEY, TENANCIES_KEY } from '../../shared/api/queryKeys';
import { useNotifications } from '../../shared/context/NotificationContext';
import { getApiErrorMessage } from '../../shared/utils/apiError';

/**
 * Runs a bill or tenancy change and refreshes everything that shows bills (lists, the tenancy page,
 * the header badge). A failure is shown as a notification; the promise still rejects for dialogs that
 * want to stay open.
 */
export function useBillActions() {
  const queryClient = useQueryClient();
  const { addNotification } = useNotifications();
  const { t } = useTranslation(['bills', 'validation']);

  const refresh = useCallback(async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: [BILLS_KEY] }),
      queryClient.invalidateQueries({ queryKey: [BILLS_SUMMARY_KEY] }),
      queryClient.invalidateQueries({ queryKey: [TENANCIES_KEY] }),
    ]);
  }, [queryClient]);

  /** For buttons: the error becomes a notification. */
  const run = useCallback(async (action: () => Promise<unknown>) => {
    try {
      await action();
    } catch (err) {
      addNotification({ title: t('bills:title'), message: getApiErrorMessage(err, t), type: 'error' });
    } finally {
      await refresh();
    }
  }, [addNotification, refresh, t]);

  /** For dialogs: the error is thrown back so the dialog shows it and stays open. */
  const runInDialog = useCallback(async (action: () => Promise<unknown>) => {
    await action();
    await refresh();
  }, [refresh]);

  return { run, runInDialog, refresh };
}
