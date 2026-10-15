/// <reference types="node" />
import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
import type { TFunction } from 'i18next';
import { getApiErrorMessage } from './apiError';
import sr from '../../locales/sr/validation.json';
import en from '../../locales/en/validation.json';
import de from '../../locales/de/validation.json';
import ru from '../../locales/ru/validation.json';

// The key itself is returned so assertions do not depend on translation text.
const t = ((key: string) => key) as unknown as TFunction;

const httpError = (status: number, data?: unknown) => ({ response: { status, data } });

describe('getApiErrorMessage', () => {
  it('reports a network problem when there is no response', () => {
    expect(getApiErrorMessage(new Error('Network Error'), t)).toBe('validation:errNetwork');
  });

  it('never leaks an HTML error page from a 5xx response', () => {
    expect(getApiErrorMessage(httpError(503, '<!DOCTYPE html><html>...'), t)).toBe('validation:errServer');
  });

  it('explains rate limiting instead of calling it invalid credentials', () => {
    expect(getApiErrorMessage(httpError(429), t, { 401: 'invalidCredentials' })).toBe('validation:errTooManyRequests');
  });

  it('applies the per-screen override for a status code', () => {
    expect(getApiErrorMessage(httpError(401), t, { 401: 'invalidCredentials' })).toBe('invalidCredentials');
    expect(getApiErrorMessage(httpError(403), t, { 403: 'emailNotVerified' })).toBe('emailNotVerified');
    expect(getApiErrorMessage(httpError(409), t, { 409: 'registrationFailed' })).toBe('registrationFailed');
  });

  it('translates a FluentValidation code from the 400 response', () => {
    const data = { errors: { Password: ['PASSWORD_NEEDS_UPPER'] } };
    expect(getApiErrorMessage(httpError(400, data), t)).toBe('validation:PASSWORD_NEEDS_UPPER');
  });

  it('translates codes from any module, not only users', () => {
    const data = { errors: { Title: ['TITLE_REQUIRED'], City: ['CITY_REQUIRED'] } };
    expect(getApiErrorMessage(httpError(400, data), t)).toBe('validation:TITLE_REQUIRED');
  });

  it('falls back to a generic validation message for text that is not a code', () => {
    const data = { errors: { Foo: ['The Foo field is required.'] } };
    expect(getApiErrorMessage(httpError(400, data), t)).toBe('validation:errValidation');
  });

  it('translates the code of a broken business rule (HTTP 422)', () => {
    const data = { message: 'INCORRECT_OLD_PASSWORD', statusCode: 422, code: 'INCORRECT_OLD_PASSWORD' };
    expect(getApiErrorMessage(httpError(422, data), t)).toBe('validation:INCORRECT_OLD_PASSWORD');
  });

  it('uses the 400 override when the body is not a validation problem', () => {
    expect(getApiErrorMessage(httpError(400, { message: 'x' }), t, { 400: 'resetTokenInvalid' })).toBe('resetTokenInvalid');
  });

  it('describes common statuses without showing server text', () => {
    expect(getApiErrorMessage(httpError(403, { message: 'You do not have permission' }), t)).toBe('validation:errForbidden');
    expect(getApiErrorMessage(httpError(404), t)).toBe('validation:errNotFound');
    expect(getApiErrorMessage(httpError(409, 'Duplicate'), t)).toBe('validation:errConflict');
  });

  it('uses the screen fallback for statuses nothing else describes', () => {
    expect(getApiErrorMessage(httpError(400, { message: 'x' }), t, {}, 'Failed to create the listing')).toBe(
      'Failed to create the listing',
    );
  });

  it('uses a generic message when there is no screen fallback', () => {
    expect(getApiErrorMessage(httpError(418), t)).toBe('validation:errGeneric');
  });
});

describe('server validation codes', () => {
  // ValidationCodes.cs is the source of truth; every code must be translated in every language.
  // Tests run with the front-land folder as the working directory.
  const codes = ['ValidationCodes.cs', 'BusinessErrorCodes.cs'].flatMap(file =>
    [
      ...readFileSync(`../LandlordApp/src/Common/Validation/${file}`, 'utf-8').matchAll(
        /public const string \w+ = "([A-Z0-9_]+)";/g,
      ),
    ].map(m => m[1]),
  );

  it('finds the codes defined on the server', () => {
    expect(codes.length).toBeGreaterThan(40);
  });

  it.each([
    ['sr', sr],
    ['en', en],
    ['de', de],
    ['ru', ru],
  ])('has a translation for every code in %s', (_lang, messages) => {
    const dictionary = messages as Record<string, string>;
    const missing = codes.filter(code => !dictionary[code]);
    expect(missing).toEqual([]);
  });

  it('has no translation left over for a code the server no longer defines', () => {
    const generic = /^err[A-Z]/;
    const extra = Object.keys(sr).filter(key => !generic.test(key) && !codes.includes(key));
    expect(extra).toEqual([]);
  });
});
