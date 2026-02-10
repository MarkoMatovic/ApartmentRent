import { apiClient } from './client';
import { SubscriptionPlan } from '../types/subscription';

export const paymentsApi = {
    /**
     * Get available subscription plans
     */
    getPlans: async (): Promise<SubscriptionPlan[]> => {
        const response = await apiClient.get('/api/payments/plans');
        return response.data;
    },

    /**
     * Create Stripe checkout session
     */
    createCheckoutSession: async (priceId: string, successUrl: string, cancelUrl: string): Promise<{ url: string }> => {
        const response = await apiClient.post('/api/payments/create-checkout-session', {
            priceId,
            successUrl,
            cancelUrl
        });
        return response.data;
    }
};
