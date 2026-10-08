import { describe, it, expect, vi, beforeEach } from 'vitest';

const client = vi.hoisted(() => ({ get: vi.fn(), post: vi.fn() }));
vi.mock('./client', () => ({ apiClient: client }));

import { paymentsApi } from './paymentsApi';

beforeEach(() => {
  client.get.mockReset();
  client.post.mockReset();
});

describe('paymentsApi.createPayment', () => {
  it('sends only the plan — buyer identity and price are decided server-side', async () => {
    client.post.mockResolvedValue({ data: { transactionId: 'txn_1' } });

    const result = await paymentsApi.createPayment('tokens-50');

    expect(client.post).toHaveBeenCalledWith('/api/payments/create-payment', { planId: 'tokens-50' });
    expect(result.transactionId).toBe('txn_1');
  });

  it('includes the apartment for featured plans', async () => {
    client.post.mockResolvedValue({ data: { transactionId: 'txn_2' } });

    await paymentsApi.createPayment('featured-7', 42);

    expect(client.post).toHaveBeenCalledWith('/api/payments/create-payment', { planId: 'featured-7', apartmentId: 42 });
  });
});

describe('paymentsApi.getPaddleConfig', () => {
  it('returns the public checkout config', async () => {
    client.get.mockResolvedValue({ data: { environment: 'sandbox', clientToken: 'test_x', enabled: true } });

    await expect(paymentsApi.getPaddleConfig()).resolves.toEqual({
      environment: 'sandbox',
      clientToken: 'test_x',
      enabled: true,
    });
    expect(client.get).toHaveBeenCalledWith('/api/payments/paddle-config');
  });
});
