import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { AxiosError, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios';
import { apiClient, refreshAccessToken } from './client';
import { getAccessToken, setAccessToken } from './tokenStore';

type Handler = (config: InternalAxiosRequestConfig) => { status: number; data?: unknown };

/** Answers requests in memory; a status of 400 or more is rejected the way axios does it. */
const serve = (handler: Handler) => {
  const calls: InternalAxiosRequestConfig[] = [];
  apiClient.defaults.adapter = async (config) => {
    calls.push(config);
    await new Promise(resolve => setTimeout(resolve, 5)); // a request takes a moment
    const { status, data = '' } = handler(config);
    const response = { data, status, statusText: '', headers: {}, config } as AxiosResponse;
    if (status >= 400) throw new AxiosError('failed', String(status), config, null, response);
    return response;
  };
  return calls;
};

const isRefresh = (config: InternalAxiosRequestConfig) => config.url?.includes('token/refresh') ?? false;

const originalAdapter = apiClient.defaults.adapter;

beforeEach(() => setAccessToken(null));
afterEach(() => {
  apiClient.defaults.adapter = originalAdapter;
  setAccessToken(null);
});

describe('refreshAccessToken', () => {
  it('sends one refresh for callers that ask at the same time, and they all get its token', async () => {
    let issued = 0;
    const calls = serve(() => ({ status: 200, data: { accessToken: `token-${++issued}` } }));

    const tokens = await Promise.all([refreshAccessToken(), refreshAccessToken(), refreshAccessToken()]);

    expect(calls.filter(isRefresh)).toHaveLength(1);
    expect(tokens).toEqual(['token-1', 'token-1', 'token-1']);
    expect(getAccessToken()).toBe('token-1');
  });

  it('sends a new refresh once the previous one has finished', async () => {
    let issued = 0;
    const calls = serve(() => ({ status: 200, data: { accessToken: `token-${++issued}` } }));

    await refreshAccessToken();
    const second = await refreshAccessToken();

    expect(calls.filter(isRefresh)).toHaveLength(2);
    expect(second).toBe('token-2');
  });

  it('lets the next call try again after a failed refresh', async () => {
    let fail = true;
    serve(() => (fail ? { status: 401, data: { message: 'expired' } } : { status: 200, data: { accessToken: 'fresh' } }));

    await expect(refreshAccessToken()).rejects.toBeInstanceOf(AxiosError);
    fail = false;

    await expect(refreshAccessToken()).resolves.toBe('fresh');
  });
});

describe('apiClient on 401', () => {
  it('requests that fail together share one refresh and are all repeated with the new token', async () => {
    const calls = serve(config => {
      if (isRefresh(config)) return { status: 200, data: { accessToken: 'fresh' } };
      return config.headers.Authorization === 'Bearer fresh' ? { status: 200, data: config.url } : { status: 401 };
    });

    const results = await Promise.all(['/a', '/b', '/c'].map(url => apiClient.get(url)));

    expect(results.map(r => r.data)).toEqual(['/a', '/b', '/c']);
    expect(calls.filter(isRefresh)).toHaveLength(1);
  });
});
