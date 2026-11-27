import { EnvironmentProviders, importProvidersFrom } from '@angular/core';
import { TranslocoTestingModule } from '@jsverse/transloco';
import en from '../../../public/i18n/en.json';

/** Transloco with the real English strings, synchronously, for component tests. */
export function provideTranslocoForTests(): EnvironmentProviders {
  return importProvidersFrom(
    TranslocoTestingModule.forRoot({
      langs: { en },
      translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
      preloadLangs: true,
    }),
  );
}
