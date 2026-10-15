import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '../shared/context/AuthContext';
import { roommatePairingApi, MyPairingState } from '../shared/api/roommatePairing';
import { MY_PAIRING_KEY } from '../shared/api/queryKeys';

const EMPTY: MyPairingState = { active: null, incoming: [], outgoing: [] };

/**
 * The signed-in user's roommate situation: their confirmed roommate (if any) and the open
 * proposals. Shared by the header, the apartment page, the profile of a roommate and the chat, so
 * they all agree on whether the user is "taken".
 */
export const useMyPairing = () => {
  const { user, isAuthenticated } = useAuth();
  const queryClient = useQueryClient();
  const userId = user?.userId;

  const query = useQuery({
    queryKey: [MY_PAIRING_KEY, userId],
    queryFn: roommatePairingApi.getMine,
    enabled: isAuthenticated && !!userId,
    staleTime: 30_000,
  });

  const state = query.data ?? EMPTY;

  return {
    ...state,
    isLoading: query.isLoading,
    isError: query.isError,
    /** The confirmed roommate, or null. */
    partner: state.active,
    hasRoommate: state.active !== null,
    refresh: () => queryClient.invalidateQueries({ queryKey: [MY_PAIRING_KEY] }),
  };
};
