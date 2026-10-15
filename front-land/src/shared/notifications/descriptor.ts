import type { TFunction } from 'i18next';

/**
 * What the server sends next to the Serbian title and message of a live notification: a translation
 * key with its values, and the page to open when the notification is clicked.
 */
export interface NotificationDescriptor {
  /** Id of the stored notification: lets the browser mark it read on the server and not show it twice. */
  id?: number;
  key?: string;
  params?: Record<string, string | number>;
  link?: string;
}

/** Only in-app routes are followed: never a full URL, a protocol-relative URL or an id. */
export const isInAppRoute = (link: unknown): link is string =>
  typeof link === 'string' && link.startsWith('/') && !link.startsWith('//');

const KEY = /^[a-z][a-z0-9_]*$/i;

/** Reads the 5th argument of "ReceiveNotification". Anything unexpected is ignored, never thrown. */
export function parseDescriptor(raw: unknown): NotificationDescriptor | undefined {
  if (typeof raw !== 'string' || raw.length === 0) return undefined;
  try {
    const data = JSON.parse(raw) as Record<string, unknown> | null;
    if (!data || typeof data !== 'object') return undefined;

    const descriptor: NotificationDescriptor = {};
    if (typeof data.id === 'number' && Number.isInteger(data.id) && data.id > 0) descriptor.id = data.id;
    if (typeof data.key === 'string' && KEY.test(data.key)) descriptor.key = data.key;
    if (isInAppRoute(data.link)) descriptor.link = data.link;
    if (data.params && typeof data.params === 'object') {
      const params: Record<string, string | number> = {};
      for (const [name, value] of Object.entries(data.params as Record<string, unknown>)) {
        if (typeof value === 'string' || typeof value === 'number') params[name] = value;
      }
      descriptor.params = params;
    }
    return descriptor.id || descriptor.key || descriptor.link ? descriptor : undefined;
  } catch {
    return undefined;
  }
}

/** Serbian is written in Latin script in the app, but the plain "sr" locale formats dates in Cyrillic. */
export const intlLocale = (language: string): string => (language === 'sr' ? 'sr-Latn' : language);

// Amounts of money: written the way the language writes numbers (1.200 in Serbian, 1,200 in English).
const AMOUNT_PARAMS = new Set(['price', 'oldRent', 'newRent', 'drop']);
// Bills are paid to the para: 3.596,13.
const MONEY_PARAMS = new Set(['amount', 'share']);

/**
 * Values from the server that only the browser can write correctly for the user's language:
 * dates (`when`, an ISO local time) and amounts. Anything else is used as it came.
 */
export function localizeParams(
  params: Record<string, string | number>,
  language: string,
): Record<string, string | number> {
  const locale = intlLocale(language);
  const result: Record<string, string | number> = { ...params };

  const when = params.when;
  if (typeof when === 'string') {
    const date = new Date(when);
    if (!Number.isNaN(date.getTime())) {
      result.when = new Intl.DateTimeFormat(locale, { dateStyle: 'medium', timeStyle: 'short' }).format(date);
    }
  }

  for (const name of AMOUNT_PARAMS) {
    const value = Number(params[name]);
    if (params[name] !== undefined && Number.isFinite(value)) {
      result[name] = new Intl.NumberFormat(locale, { maximumFractionDigits: 0 }).format(value);
    }
  }

  for (const name of MONEY_PARAMS) {
    const value = Number(params[name]);
    if (params[name] !== undefined && Number.isFinite(value)) {
      result[name] = new Intl.NumberFormat(locale, { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value);
    }
  }

  // A due date ("2026-10-15") is a calendar day: no time, and not shifted by the time zone.
  const due = params.due;
  if (typeof due === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(due)) {
    const [y, m, d] = due.split('-').map(Number);
    result.due = new Intl.DateTimeFormat(locale, { dateStyle: 'medium' }).format(new Date(y, m - 1, d));
  }
  return result;
}

/**
 * The text to show: the user's own translation of the key (`notifications:events.<key>`), filled with
 * the values from the server; the server's Serbian text when there is no key or no translation.
 */
export function describeNotification(
  t: TFunction,
  notification: { title: string; message: string; descriptor?: NotificationDescriptor },
  language = 'sr',
): { title: string; message: string } {
  const key = notification.descriptor?.key;
  if (!key) return { title: notification.title, message: notification.message };

  const params = localizeParams(notification.descriptor?.params ?? {}, language);
  return {
    title: t(`notifications:events.${key}.title`, { ...params, defaultValue: notification.title }),
    message: t(`notifications:events.${key}.message`, { ...params, defaultValue: notification.message }),
  };
}
