/** Production placeholders. Week 9 injects real values at build time from Terraform outputs. */
export const environment = {
  production: true,
  apiBaseUrl: 'https://api.REPLACE_AT_DEPLOY.example',
  oidc: {
    issuer: 'https://auth.REPLACE_AT_DEPLOY.example/realms/smartfactory',
    clientId: 'smartfactory-web',
    scope: 'openid profile email',
    requireHttps: true,
  },
} as const;
