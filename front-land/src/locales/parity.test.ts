import { describe, it, expect } from 'vitest';

// Every locale must define the same keys in every namespace, or a language silently falls back to
// raw keys / English. Plural suffixes are ignored because languages need different plural forms
// (Russian has _few/_many, English only _one/_other).
const modules = import.meta.glob('./*/*.json', { eager: true, import: 'default' }) as Record<
  string,
  Record<string, unknown>
>;

const LANGUAGES = ['en', 'sr', 'de', 'ru'];
const PLURAL = /_(zero|one|two|few|many|other)$/;

const flatten = (obj: Record<string, unknown>, prefix = ''): string[] =>
  Object.entries(obj).flatMap(([key, value]) => {
    const path = prefix ? `${prefix}.${key}` : key;
    return value && typeof value === 'object' ? flatten(value as Record<string, unknown>, path) : [path];
  });

const keysByNamespace: Record<string, Record<string, Set<string>>> = {};
for (const [path, json] of Object.entries(modules)) {
  const match = path.match(/\.\/([^/]+)\/([^/]+)\.json$/);
  if (!match) continue;
  const [, lang, namespace] = match;
  (keysByNamespace[namespace] ??= {})[lang] = new Set(flatten(json).map((k) => k.replace(PLURAL, '')));
}

describe('translation key parity', () => {
  for (const [namespace, byLang] of Object.entries(keysByNamespace)) {
    it(`${namespace}: all ${LANGUAGES.length} languages define the same keys`, () => {
      const all = new Set(Object.values(byLang).flatMap((keys) => [...keys]));
      const gaps: Record<string, string[]> = {};
      for (const lang of LANGUAGES) {
        const present = byLang[lang];
        const missing = present ? [...all].filter((k) => !present.has(k)).sort() : ['<<namespace file missing>>'];
        if (missing.length > 0) gaps[lang] = missing;
      }
      expect(gaps).toEqual({});
    });
  }
});
