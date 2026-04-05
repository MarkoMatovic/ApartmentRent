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
    getPlans: async (): Promise<SubscriptionPlan[]> => {
        const response = await apiClient.get('/api/payments/plans');
        return response.data;
    },

    createPayment: async (planId: string, successUrl: string, failureUrl: string): Promise<MonriPaymentFormFields> => {
        const response = await apiClient.post('/api/payments/create-payment', {
            planId,
            successUrl,
            failureUrl
        });
        return response.data;
    },

    refreshToken: async (): Promise<string> => {
        const response = await apiClient.post('/api/v1/auth/refresh-token');
        return response.data;
    }
};
