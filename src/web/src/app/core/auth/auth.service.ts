import { computed, inject, Injectable, signal } from '@angular/core';
import { AuthConfig, OAuthService } from 'angular-oauth2-oidc';
import { environment } from '../../../environments/environment';
import { displayName, KeycloakClaims, parsePersonaRoles } from './claims';

/**
 * OIDC Authorization Code + PKCE against Keycloak (ADR-008). Public client: no secret in the browser.
 * The SPA never validates tokens itself; it carries the access token to the Platform API, which does.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly oauth = inject(OAuthService);

  private readonly claimsSignal = signal<KeycloakClaims | null>(null);
  private readonly readySignal = signal(false);

  /** Discovery document loaded and initial login attempt finished (either way). */
  readonly ready = this.readySignal.asReadonly();
  readonly claims = this.claimsSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.claimsSignal() !== null);
  readonly displayName = computed(() => displayName(this.claimsSignal()));
  readonly tenantId = computed(() => this.claimsSignal()?.tenant_id ?? null);
  readonly roles = computed(() => parsePersonaRoles(this.claimsSignal()));

  private static readonly config: AuthConfig = {
    issuer: environment.oidc.issuer,
    clientId: environment.oidc.clientId,
    scope: environment.oidc.scope,
    responseType: 'code',
    redirectUri: `${globalThis.location?.origin ?? ''}/`,
    postLogoutRedirectUri: `${globalThis.location?.origin ?? ''}/`,
    requireHttps: environment.oidc.requireHttps,
    // Why: refresh tokens via the token endpoint; the silent-refresh iframe is a legacy implicit-flow mechanism.
    useSilentRefresh: false,
    showDebugInformation: !environment.production,
    clearHashAfterLogin: true,
  };

  /** Call once at startup (APP_INITIALIZER). Never throws: an unreachable IdP leaves the app signed out. */
  async initialize(): Promise<void> {
    this.oauth.configure(AuthService.config);
    this.oauth.events.subscribe(() => this.refreshClaims());
    try {
      await this.oauth.loadDiscoveryDocumentAndTryLogin();
      this.oauth.setupAutomaticSilentRefresh();
    } catch (error: unknown) {
      console.warn('[auth] identity provider not reachable; continuing signed out', error);
    } finally {
      this.refreshClaims();
      this.readySignal.set(true);
    }
  }

  login(returnTo: string = globalThis.location?.pathname ?? '/'): void {
    this.oauth.initCodeFlow(returnTo);
  }

  logout(): void {
    this.oauth.logOut();
  }

  /** Where to go after a redirect login; set by {@link login}. */
  consumeReturnTo(): string | null {
    const state = this.oauth.state;
    return state ? decodeURIComponent(state) : null;
  }

  accessTokenExpiresAt(): Date | null {
    const exp = this.oauth.getAccessTokenExpiration();
    return exp ? new Date(exp) : null;
  }

  private refreshClaims(): void {
    if (this.oauth.hasValidAccessToken()) {
      this.claimsSignal.set(this.oauth.getIdentityClaims() as KeycloakClaims);
    } else {
      this.claimsSignal.set(null);
    }
  }
}
