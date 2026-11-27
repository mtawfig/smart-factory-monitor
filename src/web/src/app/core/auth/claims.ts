/** Shape of the claims we read from a Keycloak token. Everything else is ignored. */
export interface KeycloakClaims {
  readonly sub?: string;
  readonly preferred_username?: string;
  readonly email?: string;
  readonly name?: string;
  readonly tenant_id?: string;
  readonly realm_access?: { readonly roles?: readonly unknown[] };
  readonly exp?: number;
}

/** The persona roles defined in the Keycloak realm (and in the Platform API's Roles class). */
export const PERSONA_ROLES = [
  'reliability-manager',
  'maintenance-planner',
  'technician',
  'ml-engineer',
  'administrator',
] as const;
export type PersonaRole = (typeof PERSONA_ROLES)[number];

/** Realm roles from `realm_access.roles`, filtered to the persona roles, sorted. */
export function parsePersonaRoles(claims: KeycloakClaims | null | undefined): PersonaRole[] {
  const raw = claims?.realm_access?.roles ?? [];
  const known = new Set<string>(PERSONA_ROLES);
  return raw.filter((r): r is PersonaRole => typeof r === 'string' && known.has(r)).sort();
}

/** Display name: full name, then username, then subject. */
export function displayName(claims: KeycloakClaims | null | undefined): string {
  return claims?.name || claims?.preferred_username || claims?.sub || 'unknown';
}
