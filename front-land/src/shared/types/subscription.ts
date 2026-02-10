export interface SubscriptionPlan {
    name: string;
    description: string;
    price: number;
    currency: string;
    stripePriceId: string;
    interval: string;
}

export interface CheckoutSession {
    sessionId: string;
    checkoutUrl: string;
}

export interface CreateCheckoutRequest {
    successUrl: string;
    cancelUrl: string;
}
