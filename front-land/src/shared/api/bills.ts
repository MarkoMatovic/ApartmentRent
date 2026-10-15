import apiClient from './client';

export interface BillingPerson {
  userId: number;
  name: string;
  profilePicture?: string | null;
}

export interface TenancyMember extends BillingPerson {
  joinedDate: string;
  leftDate?: string | null;
}

export interface Tenancy {
  tenancyId: number;
  apartmentId: number;
  apartmentTitle: string;
  landlord: BillingPerson;
  /** The signed-in user rents this apartment out. */
  isLandlord: boolean;
  monthlyRent: number;
  rentDueDay: number;
  payeeName: string;
  payeeAccount: string;
  startDate: string;
  endDate?: string | null;
  status: 'active' | 'ended';
  members: TenancyMember[];
  openBills: number;
}

export interface TenancyCandidate extends BillingPerson {
  reason: 'application' | 'roommate';
}

export type ShareStatus = 'unpaid' | 'marked_paid' | 'confirmed';
export type BillStatus = 'open' | 'partly_paid' | 'paid' | 'cancelled';
export type BillKind = 'rent' | 'electricity' | 'water' | 'infostan' | 'internet' | 'heating' | 'gas' | 'other';
export const UTILITY_KINDS: BillKind[] = ['electricity', 'water', 'infostan', 'internet', 'heating', 'gas', 'other'];

export interface BillShare {
  billShareId: number;
  payer: BillingPerson;
  receiver: BillingPerson;
  amount: number;
  status: ShareStatus;
  markedPaidAt?: string | null;
  confirmedAt?: string | null;
  canMarkPaid: boolean;
  canConfirm: boolean;
}

export interface Bill {
  billId: number;
  tenancyId: number;
  apartmentTitle: string;
  /** Tenants living there now: a company bill is split among them. */
  tenantCount: number;
  kind: BillKind;
  title: string;
  amount: number;
  dueDate: string;
  period?: string | null;
  payTo: 'landlord' | 'company';
  payeeName: string;
  payeeAccount: string;
  paymentCode: string;
  paymentModel?: string | null;
  paymentReference?: string | null;
  purpose: string;
  createdBy: BillingPerson;
  createdDate: string;
  status: BillStatus;
  isOverdue: boolean;
  paidBy?: BillingPerson | null;
  paidAt?: string | null;
  canPayCompany: boolean;
  canUndoCompanyPayment: boolean;
  canEdit: boolean;
  shares: BillShare[];
}

export interface PaymentDue {
  type: 'share' | 'company_bill';
  billShareId?: number | null;
  bill: Bill;
  amount: number;
  receiver?: BillingPerson | null;
  status: ShareStatus | 'unpaid';
}

export interface MyBills {
  toPay: PaymentDue[];
  toConfirm: { share: BillShare; bill: Bill }[];
}

export interface BillingSummary {
  toPay: number;
  toConfirm: number;
  amountToPay: number;
}

export interface PaymentInstructions {
  payeeName: string;
  payeeAccount: string;
  amount: number;
  currency: 'RSD';
  paymentCode: string;
  purpose: string;
  paymentModel?: string | null;
  paymentReference?: string | null;
  qrPayload: string;
  qrCodeDataUrl: string;
}

export interface BillingProfile {
  payoutName?: string | null;
  payoutAccount?: string | null;
}

export interface BillPage {
  totalCount: number;
  page: number;
  pageSize: number;
  bills: Bill[];
}

export interface TenancyInput {
  monthlyRent: number;
  rentDueDay: number;
  payeeName: string;
  payeeAccount: string;
}

export interface CreateTenancyInput extends TenancyInput {
  apartmentId: number;
  tenantUserIds: number[];
  startDate: string;
}

/** The server takes enum names ("Electricity"); the client works with lowercase keys. */
const kindName = (kind: BillKind) => kind.charAt(0).toUpperCase() + kind.slice(1);

export interface CreateBillInput {
  tenancyId: number;
  kind: BillKind;
  title: string;
  amount: number;
  dueDate: string;
  payTo: 'landlord' | 'company';
  payeeName?: string;
  payeeAccount?: string;
  paymentCode?: string;
  paymentModel?: string;
  paymentReference?: string;
}

export interface UpdateBillInput {
  title: string;
  amount: number;
  dueDate: string;
}

const tenancies = '/api/v1/tenancies';
const bills = '/api/v1/bills';

export const billsApi = {
  // ── tenancies ──
  getTenancies: async (): Promise<Tenancy[]> => (await apiClient.get<Tenancy[]>(tenancies)).data,
  getTenancy: async (id: number): Promise<Tenancy> => (await apiClient.get<Tenancy>(`${tenancies}/${id}`)).data,
  getCandidates: async (apartmentId: number): Promise<TenancyCandidate[]> =>
    (await apiClient.get<TenancyCandidate[]>(`${tenancies}/candidates`, { params: { apartmentId } })).data,
  createTenancy: async (input: CreateTenancyInput): Promise<Tenancy> =>
    (await apiClient.post<Tenancy>(tenancies, input)).data,
  updateTenancy: async (id: number, input: TenancyInput): Promise<Tenancy> =>
    (await apiClient.put<Tenancy>(`${tenancies}/${id}`, input)).data,
  addTenant: async (id: number, userId: number): Promise<Tenancy> =>
    (await apiClient.post<Tenancy>(`${tenancies}/${id}/tenants`, { userId })).data,
  /** The landlord removes a tenant; a tenant passes their own id to leave. */
  removeTenant: async (id: number, userId: number): Promise<Tenancy> =>
    (await apiClient.delete<Tenancy>(`${tenancies}/${id}/tenants/${userId}`)).data,
  endTenancy: async (id: number): Promise<Tenancy> => (await apiClient.post<Tenancy>(`${tenancies}/${id}/end`)).data,
  getTenancyBills: async (id: number, page = 1, pageSize = 20): Promise<BillPage> =>
    (await apiClient.get<BillPage>(`${tenancies}/${id}/bills`, { params: { page, pageSize } })).data,

  // ── bills ──
  getMine: async (): Promise<MyBills> => (await apiClient.get<MyBills>(`${bills}/mine`)).data,
  getSummary: async (): Promise<BillingSummary> => (await apiClient.get<BillingSummary>(`${bills}/summary`)).data,
  createBill: async (input: CreateBillInput): Promise<Bill> =>
    (await apiClient.post<Bill>(bills, {
      ...input,
      kind: kindName(input.kind),
      payTo: input.payTo === 'landlord' ? 'Landlord' : 'Company',
    })).data,
  updateBill: async (id: number, input: UpdateBillInput): Promise<Bill> =>
    (await apiClient.put<Bill>(`${bills}/${id}`, input)).data,
  cancelBill: async (id: number): Promise<void> => {
    await apiClient.post(`${bills}/${id}/cancel`);
  },
  getCompanyPayment: async (id: number): Promise<PaymentInstructions> =>
    (await apiClient.get<PaymentInstructions>(`${bills}/${id}/payment`)).data,
  payCompanyBill: async (id: number, payout?: { payoutName?: string; payoutAccount?: string }): Promise<Bill> =>
    (await apiClient.post<Bill>(`${bills}/${id}/pay`, payout ?? {})).data,
  undoCompanyPayment: async (id: number): Promise<Bill> =>
    (await apiClient.post<Bill>(`${bills}/${id}/undo-payment`)).data,
  getSharePayment: async (shareId: number): Promise<PaymentInstructions> =>
    (await apiClient.get<PaymentInstructions>(`${bills}/shares/${shareId}/payment`)).data,
  markSharePaid: async (shareId: number): Promise<Bill> =>
    (await apiClient.post<Bill>(`${bills}/shares/${shareId}/mark-paid`)).data,
  unmarkShare: async (shareId: number): Promise<Bill> =>
    (await apiClient.post<Bill>(`${bills}/shares/${shareId}/unmark`)).data,
  confirmShare: async (shareId: number): Promise<Bill> =>
    (await apiClient.post<Bill>(`${bills}/shares/${shareId}/confirm`)).data,
  rejectShare: async (shareId: number): Promise<Bill> =>
    (await apiClient.post<Bill>(`${bills}/shares/${shareId}/reject`)).data,
  getProfile: async (): Promise<BillingProfile> => (await apiClient.get<BillingProfile>(`${bills}/profile`)).data,
  saveProfile: async (profile: { payoutName: string; payoutAccount: string }): Promise<BillingProfile> =>
    (await apiClient.put<BillingProfile>(`${bills}/profile`, profile)).data,
};

// ── helpers the screens share ──

/**
 * The same check as the server: 18 digits (bank-account-control, the middle part padded with zeros)
 * whose number leaves 1 divided by 97. Catches a mistyped digit before the form is sent.
 */
export function isValidSerbianAccount(input: string): boolean {
  const text = input.trim().replace(/\s+/g, '-').replace(/-+/g, '-');
  let digits: string;
  const grouped = /^(\d{3})-(\d{1,13})-(\d{2})$/.exec(text);
  if (grouped) digits = grouped[1] + grouped[2].padStart(13, '0') + grouped[3];
  else if (/^\d{18}$/.test(text)) digits = text;
  else return false;
  let remainder = 0;
  for (const c of digits) remainder = (remainder * 10 + Number(c)) % 97;
  return remainder === 1;
}

/** "2026-10-15" is a calendar day: read it as local, not as UTC midnight. */
export const parseDay = (value: string): Date => {
  const [y, m, d] = value.slice(0, 10).split('-').map(Number);
  return new Date(y, m - 1, d);
};

export const intlLocale = (language: string) => (language === 'sr' ? 'sr-Latn' : language);

export const formatMoney = (amount: number, language: string) =>
  new Intl.NumberFormat(intlLocale(language), { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(amount);

export const formatDay = (value: string, language: string) =>
  new Intl.DateTimeFormat(intlLocale(language), { dateStyle: 'medium' }).format(parseDay(value));

/** Today as "yyyy-MM-dd" in the browser's own calendar. */
export const todayIso = (): string => {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;
};
