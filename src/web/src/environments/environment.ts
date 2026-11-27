/** Development environment: the Docker Compose stack on this machine. */
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5080',
  oidc: {
    issuer: 'http://localhost:8180/realms/smartfactory',
    clientId: 'smartfactory-web',
    scope: 'openid profile email',
    // Why: Keycloak runs over plain HTTP in Compose only. Production requires HTTPS.
    requireHttps: false,
  },
} as const;
