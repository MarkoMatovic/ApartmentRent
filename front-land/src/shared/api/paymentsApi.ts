import { apiClient } from './client';
import { SubscriptionPlan } from '../types/subscription';

export interface MonriPaymentFormFields {
    formAction: string;
    authenticityToken: string;
    orderNumber: string;
    amount: number;
    currency: string;
    orderInfo: string;
    digest: string;
    successUrl: string;
    failureUrl: string;
    callbackUrl: string;
    buyerEmail: string;
    buyerName: string;
}

export const paymentsApi = {
    /** Fetches available subscription/purchase plans from the server. */
    getPlans: async (): Promise<SubscriptionPlan[]> => {
        const response = await apiClient.get('/api/payments/plans');
        return response.data;
    },

    /**
     * Initiates a Monri payment for the given plan.
     * Returns a form descriptor that should be submitted as a POST to Monri's
     * hosted payment page (see submitMonriForm helper below).
     */
    /**
     * @param apartmentId Required for featured-* plans; omit for all others.
     */
    createPayment: async (
        planId: string,
        successUrl: string,
        failureUrl: string,
        apartmentId?: number,
    ): Promise<MonriPaymentFormFields> => {
        const response = await apiClient.post('/api/payments/create-payment', {
            planId,
            successUrl,
            failureUrl,
            ...(apartmentId ? { apartmentId } : {}),
        });
        return response.data;
    },
};

/**
 * Dynamically builds and submits a hidden HTML form to Monri's hosted payment
 * page.  Must be called client-side (requires document).
 */
export function submitMonriForm(fields: MonriPaymentFormFields): void {
    const form = document.createElement('form');
    form.method = 'POST';
    form.action = fields.formAction;

    const fieldMap: Record<string, string> = {
        authenticity_token:   fields.authenticityToken,
        order_number:         fields.orderNumber,
        amount:               String(fields.amount),
        currency:             fields.currency,
        order_info:           fields.orderInfo,
        digest:               fields.digest,
        success_url_override: fields.successUrl,
        failure_url_override: fields.failureUrl,
        callback_url:         fields.callbackUrl,
        buyer_name:           fields.buyerName,
        buyer_email:          fields.buyerEmail,
        language:             'sr',
    };

    Object.entries(fieldMap).forEach(([name, value]) => {
        const input = document.createElement('input');
        input.type  = 'hidden';
        input.name  = name;
        input.value = value;
        form.appendChild(input);
    });

    document.body.appendChild(form);
    form.submit();
}
