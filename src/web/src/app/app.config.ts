import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import {
  ApplicationConfig,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideTransloco } from '@jsverse/transloco';
import { provideOAuthClient } from 'angular-oauth2-oidc';
import { environment } from '../environments/environment';
import { routes } from './app.routes';
import { AuthService } from './core/auth/auth.service';
import { TranslocoHttpLoader } from './core/i18n/transloco-loader';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    // Why withInterceptorsFromDi: angular-oauth2-oidc registers a class-based interceptor that attaches
    // the access token to calls under resourceServer.allowedUrls and nothing else.
    provideHttpClient(withInterceptorsFromDi()),
    provideOAuthClient({
      resourceServer: { allowedUrls: [environment.apiBaseUrl], sendAccessToken: true },
    }),
    provideTransloco({
      config: {
        availableLangs: ['en'],
        defaultLang: 'en',
        fallbackLang: 'en',
        reRenderOnLangChange: true,
        prodMode: environment.production,
      },
      loader: TranslocoHttpLoader,
    }),
    provideAppInitializer(() => inject(AuthService).initialize()),
  ],
};
