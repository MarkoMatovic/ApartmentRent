import { useQuery } from '@tanstack/react-query';
import { useAuth } from '../shared/context/AuthContext';
import { billsApi, BillingSummary } from '../shared/api/bills';
import { BILLS_SUMMARY_KEY } from '../shared/api/queryKeys';

const EMPTY: BillingSummary = { toPay: 0, toConfirm: 0, amountToPay: 0 };

/**
 * How many bills wait for the signed-in user: to pay, and payments to them waiting for "arrived".
 * The header badge shows it; a bill notification refreshes it.
 */
export const useBillsSummary = () => {
  const { user, isAuthenticated } = useAuth();
  const query = useQuery({
    queryKey: [BILLS_SUMMARY_KEY, user?.userId],
    queryFn: billsApi.getSummary,
    enabled: isAuthenticated && !!user?.userId,
    staleTime: 60_000,
  });
  return query.data ?? EMPTY;
};
