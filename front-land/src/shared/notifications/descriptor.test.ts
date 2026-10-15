import { describe, expect, it } from 'vitest';
import type { TFunction } from 'i18next';
import { describeNotification, intlLocale, isInAppRoute, localizeParams, parseDescriptor } from './descriptor';
import sr from '../../locales/sr/notifications.json';
import en from '../../locales/en/notifications.json';
import de from '../../locales/de/notifications.json';
import ru from '../../locales/ru/notifications.json';

describe('parseDescriptor', () => {
  it('reads the key, the values and the route the server sends', () => {
    const raw = JSON.stringify({ key: 'pairing_apartment', params: { name: 'Ana Anić', title: 'Stan' }, link: '/my-roommate' });

    expect(parseDescriptor(raw)).toEqual({
      key: 'pairing_apartment',
      params: { name: 'Ana Anić', title: 'Stan' },
      link: '/my-roommate',
    });
  });

  it('reads the id of the stored notification, and ignores anything that is not a whole positive number', () => {
    expect(parseDescriptor('{"id":17,"key":"pairing_ended"}')).toEqual({ id: 17, key: 'pairing_ended' });
    expect(parseDescriptor('{"id":17}')).toEqual({ id: 17 });
    for (const id of ['"17"', '0', '-3', '1.5', 'null', '[1]']) {
      expect(parseDescriptor(`{"id":${id},"key":"pairing_ended"}`)).toEqual({ key: 'pairing_ended' });
    }
  });

  it('accepts a route without a key and a key without a route', () => {
    expect(parseDescriptor('{"link":"/appointments/manage"}')).toEqual({ link: '/appointments/manage' });
    expect(parseDescriptor('{"key":"pairing_ended"}')).toEqual({ key: 'pairing_ended' });
  });

  it.each([undefined, null, '', 'not json', '[]', '"text"', '{}', '{"params":{"name":"x"}}'])(
    'ignores %j',
    (raw) => {
      expect(parseDescriptor(raw as unknown)).toBeUndefined();
    },
  );

  it.each([
    'https://evil.example/phish',
    '//evil.example/phish',
    'javascript:alert(1)',
    '42',
    'chat',
  ])('never follows the link %s', (link) => {
    expect(parseDescriptor(JSON.stringify({ key: 'new_message', link }))).toEqual({ key: 'new_message' });
    expect(parseDescriptor(JSON.stringify({ link }))).toBeUndefined();
  });

  it('drops a key that is not a plain identifier, so it cannot reach into other translations', () => {
    expect(parseDescriptor(JSON.stringify({ key: 'x.title', link: '/chat' }))).toEqual({ link: '/chat' });
    expect(parseDescriptor(JSON.stringify({ key: '../common', link: '/chat' }))).toEqual({ link: '/chat' });
  });

  it('keeps only text and numbers as values', () => {
    const d = parseDescriptor(JSON.stringify({ key: 'new_message', params: { name: 'Ana', n: 3, bad: { a: 1 }, worse: null } }));

    expect(d?.params).toEqual({ name: 'Ana', n: 3 });
  });
});

describe('isInAppRoute', () => {
  it.each([
    ['/chat', true],
    ['/chat?userId=5', true],
    ['//x', false],
    ['x', false],
    ['', false],
  ])('%j -> %s', (link, expected) => {
    expect(isInAppRoute(link)).toBe(expected);
  });

  it('rejects anything that is not text', () => {
    expect(isInAppRoute(undefined)).toBe(false);
    expect(isInAppRoute(5)).toBe(false);
  });
});

describe('describeNotification', () => {
  // A stand-in for i18next: returns the translation for the key, or the default.
  const translations: Record<string, string> = {
    'notifications:events.pairing_accepted.title': 'Roommate agreement confirmed',
    'notifications:events.pairing_accepted.message': '{{name}} accepted.',
  };
  const t = ((key: string, options: Record<string, string>) => {
    const text = translations[key];
    if (!text) return options.defaultValue;
    return text.replace(/{{(\w+)}}/g, (_, name: string) => options[name]);
  }) as unknown as TFunction;

  it('uses the translation of the key, filled with the values from the server', () => {
    const shown = describeNotification(t, {
      title: 'Cimerstvo potvrđeno',
      message: 'Marko je prihvatio/la.',
      descriptor: { key: 'pairing_accepted', params: { name: 'Marko' } },
    });

    expect(shown).toEqual({ title: 'Roommate agreement confirmed', message: 'Marko accepted.' });
  });

  it('falls back to the server text for a key this version does not know', () => {
    const shown = describeNotification(t, {
      title: 'Nešto novo', message: 'Tekst sa servera', descriptor: { key: 'brand_new_event' },
    });

    expect(shown).toEqual({ title: 'Nešto novo', message: 'Tekst sa servera' });
  });

  it('shows the server text when there is no key at all', () => {
    expect(describeNotification(t, { title: 'A', message: 'B' })).toEqual({ title: 'A', message: 'B' });
    expect(describeNotification(t, { title: 'A', message: 'B', descriptor: { link: '/chat' } })).toEqual({ title: 'A', message: 'B' });
  });
});

describe('the translations of server notifications', () => {
  const languages = { sr, en, de, ru } as Record<string, { events: Record<string, { title: string; message: string }> }>;
  const placeholders = (text: string) => [...text.matchAll(/{{\s*(\w+)\s*(?:,[^}]*)?}}/g)].map(m => m[1]).sort();

  it('exist for every event in every language', () => {
    const events = Object.keys(sr.events).sort();
    for (const [language, file] of Object.entries(languages)) {
      expect(Object.keys(file.events).sort(), language).toEqual(events);
      for (const event of events) {
        expect(file.events[event].title, `${language}.${event}.title`).not.toBe('');
        expect(file.events[event].message, `${language}.${event}.message`).not.toBe('');
      }
    }
  });

  it('use the same values in every language, so none of them shows a raw {{placeholder}}', () => {
    for (const event of Object.keys(sr.events)) {
      const expected = placeholders(sr.events[event as keyof typeof sr.events].message);
      for (const [language, file] of Object.entries(languages)) {
        expect(placeholders(file.events[event].message), `${language}.${event}`).toEqual(expected);
      }
    }
  });

  it('only ask for values the server sends', () => {
    const sent = [
      'name', 'title', 'when', 'city', 'price', 'search', 'oldRent', 'newRent', 'drop', 'percent',
      'days', 'tokens', 'credits', 'status', 'amount', 'share', 'due', 'payee',
    ];
    for (const [language, file] of Object.entries(languages)) {
      for (const [event, text] of Object.entries(file.events)) {
        for (const name of [...placeholders(text.title), ...placeholders(text.message)]) {
          expect(sent, `${language}.${event}`).toContain(name);
        }
      }
    }
  });

  it('cover every notification the server can send', () => {
    // keep in step with the Key = "..." values in LandlordApp/src
    const serverKeys = [
      'pairing_proposed', 'pairing_accepted', 'pairing_declined', 'pairing_ended', 'pairing_void', 'pairing_apartment',
      'group_created', 'group_message', 'new_message',
      'application_new', 'application_approved', 'application_rejected', 'application_pending', 'application_cancelled', 'application_status',
      'appointment_new', 'listing_expiry_reminder', 'new_listing_match', 'price_drop', 'new_listing',
      'purchase_tokens', 'purchase_analytics', 'purchase_featured', 'purchase_credits', 'purchase_boost',
      'purchase_priority', 'purchase_generic', 'purchase_chargeback', 'purchase_refunded',
      'tenancy_added', 'tenancy_removed', 'tenancy_member_left', 'tenancy_ended',
      'bill_new', 'bill_company_new', 'bill_changed', 'bill_marked_paid', 'bill_confirmed', 'bill_rejected',
      'bill_company_paid', 'bill_company_paid_landlord', 'bill_due_soon', 'bill_overdue', 'bill_overdue_receiver',
      'bill_company_overdue',
    ];
    expect(Object.keys(sr.events).sort()).toEqual([...serverKeys].sort());
  });
});

describe('localizeParams', () => {
  it('writes the time of a viewing the way the language writes dates, in Latin script for Serbian', () => {
    const sr = localizeParams({ when: '2026-10-20T14:30:00' }, 'sr').when as string;
    expect(sr).toContain('2026');
    expect(sr).toMatch(/14[:.]30/);
    expect(sr).not.toMatch(/[Ѐ-ӿ]/);

    const en = localizeParams({ when: '2026-10-20T14:30:00' }, 'en').when as string;
    expect(en).toMatch(/Oct/);
    expect(en).toMatch(/2:30/);
  });

  it('writes amounts with the grouping of the language', () => {
    expect(localizeParams({ price: '1200' }, 'en').price).toBe('1,200');
    expect(localizeParams({ price: '1200' }, 'sr').price).toBe('1.200');
    expect(localizeParams({ oldRent: '1500', newRent: '1200', drop: '300' }, 'de'))
      .toEqual({ oldRent: '1.500', newRent: '1.200', drop: '300' });
  });

  it('writes bill amounts to the para, and a due date as a day without a time', () => {
    expect(localizeParams({ amount: '15000.5', share: '3000' }, 'sr')).toEqual({ amount: '15.000,50', share: '3.000,00' });
    expect(localizeParams({ amount: '15000.5' }, 'en').amount).toBe('15,000.50');
    expect(localizeParams({ due: '2026-10-15' }, 'en').due).toBe('Oct 15, 2026');
    expect(localizeParams({ due: '2026-10-15' }, 'sr').due).not.toMatch(/[Ѐ-ӿ]|:/);
  });

  it('leaves everything else, and what it cannot read, as it came', () => {
    expect(localizeParams({ name: 'Ana Anić', title: 'Stan 3', percent: '20', when: 'not a date', price: 'abc' }, 'en'))
      .toEqual({ name: 'Ana Anić', title: 'Stan 3', percent: '20', when: 'not a date', price: 'abc' });
  });

  it('does not change what it was given', () => {
    const params = { price: '1200' };
    localizeParams(params, 'en');
    expect(params).toEqual({ price: '1200' });
  });
});

describe('intlLocale', () => {
  it('asks for Latin Serbian, because the app writes Serbian in Latin script', () => {
    expect(intlLocale('sr')).toBe('sr-Latn');
    expect(intlLocale('en')).toBe('en');
    expect(intlLocale('de')).toBe('de');
    expect(intlLocale('ru')).toBe('ru');
  });
});

describe('describeNotification with values that need writing for the language', () => {
  it('fills the translation with the formatted values', () => {
    const t = ((key: string, options: Record<string, string>) =>
      `${key}|${options.price}|${options.when}`) as unknown as TFunction;

    const shown = describeNotification(
      t,
      { title: 'x', message: 'y', descriptor: { key: 'new_listing_match', params: { price: '1200', when: '2026-10-20T14:30:00' } } },
      'en',
    );

    expect(shown.title).toMatch(/^notifications:events\.new_listing_match\.title\|1,200\|.*Oct.*2:30/);
  });
});
