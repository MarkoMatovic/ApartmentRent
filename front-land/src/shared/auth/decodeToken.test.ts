import { describe, it, expect } from 'vitest';
import { decodeToken, ROLE_CLAIM_URI } from './decodeToken';

// Builds an unsigned token shaped like the ones TokenProvider issues (the signature is irrelevant here:
// the frontend only reads the payload for display; the server is what actually enforces access).
const tokenWith = (payload: Record<string, unknown>) => {
  const b64 = (o: unknown) => btoa(JSON.stringify(o)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${b64({ alg: 'HS256', typ: 'JWT' })}.${b64(payload)}.signature`;
};

describe('decodeToken', () => {
  it('reads the role from the full-URI claim the backend really issues', () => {
    // JsonWebTokenHandler writes ClaimTypes.Role under its URI, NOT under "role".
    const user = decodeToken(tokenWith({ userId: '7', sub: 'guid-7', [ROLE_CLAIM_URI]: 'Admin', userRoleId: '2' }));

    expect(user?.roleName).toBe('Admin');
    expect(user?.userRoleId).toBe(2);
  });

  it('still accepts a plain "role" claim', () => {
    expect(decodeToken(tokenWith({ userId: '1', role: 'Landlord' }))?.roleName).toBe('Landlord');
  });

  it('does not infer a role from the numeric role id', () => {
    const user = decodeToken(tokenWith({ userId: '1', userRoleId: '1' }));

    expect(user?.roleName).toBeUndefined(); // id 1 is "Admin" in one database and "Tenant" in another
  });

  it('collects permission claims whether sent as one value or a list', () => {
    expect(decodeToken(tokenWith({ userId: '1', permission: 'a.b' }))?.permissions).toEqual(['a.b']);
    expect(decodeToken(tokenWith({ userId: '1', permission: ['a.b', 'c.d'] }))?.permissions).toEqual(['a.b', 'c.d']);
  });

  it.each([['', 'empty string'], ['not-a-jwt', 'wrong shape'], ['a.%%%.c', 'undecodable payload']])(
    'returns null for %s (%s)',
    (token) => {
      expect(decodeToken(token)).toBeNull();
    },
  );
});
