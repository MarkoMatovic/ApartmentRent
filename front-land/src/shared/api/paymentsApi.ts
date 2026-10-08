import { apiClient } from './client';
import { SubscriptionPlan } from '../types/subscription';

/** Public Paddle.js bootstrap config returned by the backend. */
export interface PaddleConfig {
    environment: string;
    clientToken: string;
    /** False until the backend has Paddle keys configured — checkout is unavailable. */
    enabled: boolean;
}

export interface CreatePaymentResult {
    /** Paddle transaction id (txn_…) to open the inline checkout with. */
    transactionId: string;
}

export const paymentsApi = {
    /** Fetches available subscription/purchase plans from the server. */
    getPlans: async (): Promise<SubscriptionPlan[]> => {
        const response = await apiClient.get('/api/payments/plans');
        return response.data;
    },

    /** Public Paddle.js config (environment + client token). Safe to expose to the browser. */
    getPaddleConfig: async (): Promise<PaddleConfig> => {
        const response = await apiClient.get('/api/payments/paddle-config');
        return response.data;
    },

    /**
     * Creates a Paddle transaction for the given plan and returns its id.
     * custom_data (buyer, plan, apartment) is attached server-side, so it cannot be
     * tampered with here.
     * @param apartmentId Required for featured-* plans; omit for all others.
     */
    createPayment: async (planId: string, apartmentId?: number): Promise<CreatePaymentResult> => {
        const response = await apiClient.post('/api/payments/create-payment', {
            planId,
            ...(apartmentId ? { apartmentId } : {}),
        });
        return response.data;
    },
};
