import apiClient from './client';

// The backend serializes enums as their names.
export type PairingStatus = 'Pending' | 'Accepted' | 'Declined' | 'Cancelled' | 'Ended';

/** One pairing as seen by the signed-in user. */
export interface Pairing {
  pairingId: number;
  status: PairingStatus;
  partnerUserId: number;
  partnerName: string;
  partnerProfilePicture?: string | null;
  /** The other person proposed it and the signed-in user has to answer. */
  isIncoming: boolean;
  createdDate: string;
  respondedDate?: string | null;
}

export interface MyPairingState {
  /** Set once both agreed: the two are roommates and no longer available to anyone else. */
  active: Pairing | null;
  incoming: Pairing[];
  outgoing: Pairing[];
}

export interface SharedApartment {
  apartmentId: number;
  title: string;
  city?: string | null;
  address?: string | null;
  rent: number;
  price?: number | null;
  /** Who rents it out: the pair can open a shared conversation with them from the list. */
  landlordUserId?: number | null;
  landlordName?: string | null;
  /** False when the listing was deleted or has expired since it was added. */
  isAvailable: boolean;
  addedByUserId: number;
  addedByName: string;
  note?: string | null;
  addedDate: string;
}

const base = '/api/v1/roommate-pairing';

export const roommatePairingApi = {
  getMine: async (): Promise<MyPairingState> => {
    const { data } = await apiClient.get<MyPairingState>(`${base}/my`);
    return { active: data.active ?? null, incoming: data.incoming ?? [], outgoing: data.outgoing ?? [] };
  },

  propose: async (partnerUserId: number): Promise<Pairing> => {
    const { data } = await apiClient.post<Pairing>(`${base}/propose`, { partnerUserId });
    return data;
  },

  accept: async (pairingId: number): Promise<Pairing> => {
    const { data } = await apiClient.post<Pairing>(`${base}/${pairingId}/accept`);
    return data;
  },

  decline: async (pairingId: number): Promise<void> => {
    await apiClient.post(`${base}/${pairingId}/decline`);
  },

  cancel: async (pairingId: number): Promise<void> => {
    await apiClient.post(`${base}/${pairingId}/cancel`);
  },

  end: async (pairingId: number): Promise<void> => {
    await apiClient.post(`${base}/${pairingId}/end`);
  },

  getSharedApartments: async (): Promise<SharedApartment[]> => {
    const { data } = await apiClient.get<SharedApartment[]>(`${base}/shared-apartments`);
    return data;
  },

  shareApartment: async (apartmentId: number, note?: string): Promise<SharedApartment> => {
    const { data } = await apiClient.post<SharedApartment>(`${base}/shared-apartments`, {
      apartmentId,
      note: note?.trim() || undefined,
    });
    return data;
  },

  removeSharedApartment: async (apartmentId: number): Promise<void> => {
    await apiClient.delete(`${base}/shared-apartments/${apartmentId}`);
  },
};
