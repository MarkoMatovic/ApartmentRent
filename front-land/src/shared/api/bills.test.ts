import { describe, expect, it } from 'vitest';
import { formatDay, formatMoney, isValidSerbianAccount, parseDay } from './bills';

/** Control digits the way the bank (and the server) computes them: the 18-digit number leaves 1 divided by 97. */
const withControl = (sixteen: string) => {
  let remainder = 0;
  for (const c of sixteen + '00') remainder = (remainder * 10 + Number(c)) % 97;
  return sixteen + String(98 - remainder).padStart(2, '0');
};

describe('isValidSerbianAccount', () => {
  const account = withControl('160' + '0000000123456');
  const grouped = `${account.slice(0, 3)}-123456-${account.slice(16)}`;

  it('accepts the usual ways of writing a valid account', () => {
    expect(isValidSerbianAccount(account)).toBe(true);
    expect(isValidSerbianAccount(grouped)).toBe(true);
    expect(isValidSerbianAccount(grouped.replace(/-/g, ' '))).toBe(true);
    expect(isValidSerbianAccount(` ${grouped} `)).toBe(true);
  });

  it('refuses a mistyped digit, a wrong length and anything else', () => {
    const mistyped = account.slice(0, 10) + (account[10] === '9' ? '0' : String(Number(account[10]) + 1)) + account.slice(11);
    expect(isValidSerbianAccount(mistyped)).toBe(false);
    expect(isValidSerbianAccount('160-123456')).toBe(false);
    expect(isValidSerbianAccount('abc-123456-78')).toBe(false);
    expect(isValidSerbianAccount('')).toBe(false);
  });
});

describe('formatting', () => {
  it('writes dinars to the para in the reader\'s language', () => {
    expect(formatMoney(15000.5, 'sr')).toBe('15.000,50');
    expect(formatMoney(15000.5, 'en')).toBe('15,000.50');
  });

  it('reads a due date as a calendar day, not as UTC midnight', () => {
    const day = parseDay('2026-10-15');
    expect([day.getFullYear(), day.getMonth(), day.getDate()]).toEqual([2026, 9, 15]);
    expect(formatDay('2026-10-15', 'en')).toBe('Oct 15, 2026');
  });
});
