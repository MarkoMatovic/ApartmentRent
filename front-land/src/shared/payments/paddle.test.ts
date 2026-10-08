import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';

const api = vi.hoisted(() => ({ getPaddleConfig: vi.fn() }));
vi.mock('../api/paymentsApi', () => ({ paymentsApi: api }));

type PaddleGlobal = {
  Environment: { set: ReturnType<typeof vi.fn> };
  Initialize: ReturnType<typeof vi.fn>;
};

const win = window as unknown as { Paddle?: PaddleGlobal };
const fakePaddle = (): PaddleGlobal => ({ Environment: { set: vi.fn() }, Initialize: vi.fn() });

// The loader caches its init promise at module level, so every test gets a fresh copy of it.
const loadModule = async () => {
  vi.resetModules();
  return import('./paddle');
};

beforeEach(() => {
  api.getPaddleConfig.mockReset();
  delete win.Paddle;
});
afterEach(() => {
  delete win.Paddle;
});

describe('getPaddle', () => {
  it('refuses to open checkout when the backend says payments are not configured', async () => {
    api.getPaddleConfig.mockResolvedValue({ environment: 'sandbox', clientToken: '', enabled: false });
    const { getPaddle } = await loadModule();

    await expect(getPaddle()).rejects.toThrow(/nije dostupno/i);
  });

  it('initialises Paddle.js with the sandbox environment and the public client token', async () => {
    const paddle = fakePaddle();
    win.Paddle = paddle;
    api.getPaddleConfig.mockResolvedValue({ environment: 'sandbox', clientToken: 'test_abc', enabled: true });
    const { getPaddle } = await loadModule();

    await getPaddle();

    expect(paddle.Environment.set).toHaveBeenCalledWith('sandbox');
    expect(paddle.Initialize).toHaveBeenCalledWith(expect.objectContaining({ token: 'test_abc' }));
  });

  it('switches to the production environment only when the backend asks for it', async () => {
    const paddle = fakePaddle();
    win.Paddle = paddle;
    api.getPaddleConfig.mockResolvedValue({ environment: 'production', clientToken: 'live_abc', enabled: true });
    const { getPaddle } = await loadModule();

    await getPaddle();

    expect(paddle.Environment.set).toHaveBeenCalledWith('production');
  });

  it('initialises only once however many times it is requested', async () => {
    const paddle = fakePaddle();
    win.Paddle = paddle;
    api.getPaddleConfig.mockResolvedValue({ environment: 'sandbox', clientToken: 'test_abc', enabled: true });
    const { getPaddle } = await loadModule();

    await Promise.all([getPaddle(), getPaddle(), getPaddle()]);

    expect(paddle.Initialize).toHaveBeenCalledTimes(1);
    expect(api.getPaddleConfig).toHaveBeenCalledTimes(1);
  });

  it('allows a retry after a failed initialisation', async () => {
    win.Paddle = fakePaddle();
    api.getPaddleConfig
      .mockRejectedValueOnce(new Error('network'))
      .mockResolvedValueOnce({ environment: 'sandbox', clientToken: 'test_abc', enabled: true });
    const { getPaddle } = await loadModule();

    await expect(getPaddle()).rejects.toThrow('network');
    await expect(getPaddle()).resolves.toBeDefined();
  });

  it('fans checkout events out to subscribers until they unsubscribe', async () => {
    const paddle = fakePaddle();
    win.Paddle = paddle;
    api.getPaddleConfig.mockResolvedValue({ environment: 'sandbox', clientToken: 'test_abc', enabled: true });
    const { getPaddle, onPaddleEvent } = await loadModule();
    const handler = vi.fn();
    const unsubscribe = onPaddleEvent(handler);

    await getPaddle();
    const { eventCallback } = paddle.Initialize.mock.calls[0][0] as { eventCallback: (e: unknown) => void };

    eventCallback({ name: 'checkout.completed' });
    expect(handler).toHaveBeenCalledWith({ name: 'checkout.completed' });

    unsubscribe();
    eventCallback({ name: 'checkout.closed' });
    expect(handler).toHaveBeenCalledTimes(1);
  });
});
