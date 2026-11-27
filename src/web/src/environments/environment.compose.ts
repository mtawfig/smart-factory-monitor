/**
 * The containerised web app in Docker Compose (`docker compose up`): an optimised build that still talks to the
 * local stack over plain HTTP. Selected with `ng build --configuration compose`.
 */
export const environment = {
  production: true,
  apiBaseUrl: 'http://localhost:5080',
  oidc: {
    issuer: 'http://localhost:8180/realms/smartfactory',
    clientId: 'smartfactory-web',
    scope: 'openid profile email',
    // Why: Keycloak runs over plain HTTP in Compose only. Production requires HTTPS.
    requireHttps: false,
  },
} as const;
