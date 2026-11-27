import { displayName, parsePersonaRoles } from './claims';

describe('parsePersonaRoles', () => {
  it('keeps only known persona roles, sorted', () => {
    const roles = parsePersonaRoles({
      realm_access: {
        roles: ['technician', 'offline_access', 'administrator', 'uma_authorization'],
      },
    });
    expect(roles).toEqual(['administrator', 'technician']);
  });

  it('tolerates missing or malformed realm_access', () => {
    expect(parsePersonaRoles(null)).toEqual([]);
    expect(parsePersonaRoles({})).toEqual([]);
    expect(parsePersonaRoles({ realm_access: { roles: [1, null, 'ml-engineer'] } })).toEqual([
      'ml-engineer',
    ]);
  });
});

describe('displayName', () => {
  it('prefers full name, then username, then subject', () => {
    expect(displayName({ name: 'Omar Haddad', preferred_username: 'planner' })).toBe('Omar Haddad');
    expect(displayName({ preferred_username: 'planner', sub: 'abc' })).toBe('planner');
    expect(displayName({ sub: 'abc' })).toBe('abc');
    expect(displayName(undefined)).toBe('unknown');
  });
});
