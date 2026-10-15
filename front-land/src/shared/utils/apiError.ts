import type { TFunction } from 'i18next';

interface ApiErrorLike {
  response?: { status?: number; data?: unknown };
}

// FluentValidation failures on the server use a stable code as the message
// (e.g. "PASSWORD_NEEDS_UPPER"), see LandlordApp/src/Common/Validation/ValidationCodes.cs; broken
// business rules use BusinessErrorCodes.cs the same way.
// The rules and their wording live only on the server; the texts live in the `validation`
// namespace of every language.
const VALIDATION_CODE = /^[A-Z][A-Z0-9_]+$/;

function firstValidationMessage(data: unknown): string | null {
  const errors = (data as { errors?: Record<string, unknown> } | null)?.errors;
  if (!errors || typeof errors !== 'object') return null;
  for (const value of Object.values(errors)) {
    const first = Array.isArray(value) ? value[0] : value;
    if (typeof first === 'string') return first;
  }
  return null;
}

/**
 * Turns any failed request into a short, translated message. It never shows server text
 * directly: that text is English, may be an HTML error page, or may be a whole JSON object.
 *
 * - `overrides`: give a screen-specific meaning to a status code (e.g. 401 on login).
 * - `fallback`: already translated text for failures nothing else describes (e.g. "Failed to
 *   create the listing").
 */
export function getApiErrorMessage(
  err: unknown,
  t: TFunction,
  overrides: Partial<Record<number, string>> = {},
  fallback?: string,
): string {
  const response = (err as ApiErrorLike | null)?.response;
  if (!response) return t('validation:errNetwork');

  const status = response.status ?? 0;
  if (status === 429) return t('validation:errTooManyRequests');
  if (status >= 500) return t('validation:errServer');

  // Business-rule failures (HTTP 422) carry a `code` field, see BusinessErrorCodes.cs.
  const businessCode = (response.data as { code?: unknown } | null)?.code;
  if (typeof businessCode === 'string' && VALIDATION_CODE.test(businessCode)) {
    return t(`validation:${businessCode}`, { defaultValue: t('validation:errGeneric') });
  }

  if (status === 400) {
    const message = firstValidationMessage(response.data);
    if (message) {
      // Unknown or untranslated codes fall back to a generic validation message.
      return VALIDATION_CODE.test(message)
        ? t(`validation:${message}`, { defaultValue: t('validation:errValidation') })
        : t('validation:errValidation');
    }
  }

  const override = overrides[status];
  if (override) return override;

  switch (status) {
    case 401:
      return t('validation:errUnauthorized');
    case 403:
      return t('validation:errForbidden');
    case 404:
      return t('validation:errNotFound');
    case 409:
      return t('validation:errConflict');
    default:
      return fallback ?? t('validation:errGeneric');
  }
}

/** The business-rule code (the `code` field of an HTTP 422) of a failed request, or null. */
export function apiErrorCode(err: unknown): string | null {
  const code = ((err as ApiErrorLike | null)?.response?.data as { code?: unknown } | null)?.code;
  return typeof code === 'string' && VALIDATION_CODE.test(code) ? code : null;
}

export const isApiErrorCode = (err: unknown, code: string): boolean => apiErrorCode(err) === code;
