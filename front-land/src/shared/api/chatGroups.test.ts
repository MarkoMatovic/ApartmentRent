import { describe, expect, it } from 'vitest';
import { groupTitle } from './chatGroups';

const members = [
  { userId: 1, name: 'Ana Anić' },
  { userId: 2, name: 'Marko Marković' },
  { userId: 3, name: 'Lazar Lazić' },
];

describe('groupTitle', () => {
  it('names everybody except me', () => {
    expect(groupTitle({ members }, 1)).toBe('Marko Marković, Lazar Lazić');
    expect(groupTitle({ members }, 3)).toBe('Ana Anić, Marko Marković');
  });

  it('skips members whose name is unknown', () => {
    expect(groupTitle({ members: [...members, { userId: 4, name: '' }] }, 1)).toBe('Marko Marković, Lazar Lazić');
  });

  it('is empty when I am alone in the group', () => {
    expect(groupTitle({ members: [members[0]] }, 1)).toBe('');
  });
});
