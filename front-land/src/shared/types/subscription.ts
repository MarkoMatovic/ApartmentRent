/** Plan returned by GET /api/payments/plans */
export interface SubscriptionPlan {
    planId: string;       // e.g. "analytics-monthly", "tokens-50"
    name: string;
    description: string;
    price: number;        // major currency units (e.g. 4.99)
    currency: string;     // "EUR"
    interval: string;     // "month" | "year" | "one-time"
}
