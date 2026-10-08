import { describe, it, expect, beforeEach } from 'vitest';
import { getAccessToken, setAccessToken } from './tokenStore';

describe('tokenStore', () => {
  beforeEach(() => setAccessToken(null));

  it('starts empty', () => {
    expect(getAccessToken()).toBeNull();
  });

  it('keeps the token in memory only', () => {
    setAccessToken('header.payload.signature');
    expect(getAccessToken()).toBe('header.payload.signature');
    // The whole point of this module: the access token must never reach web storage (XSS-readable).
    expect(JSON.stringify({ ...localStorage })).not.toContain('header.payload.signature');
    expect(JSON.stringify({ ...sessionStorage })).not.toContain('header.payload.signature');
  });

  it('can be cleared on logout', () => {
    setAccessToken('abc');
    setAccessToken(null);
    expect(getAccessToken()).toBeNull();
  });
});
