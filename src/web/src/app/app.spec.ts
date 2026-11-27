import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideOAuthClient } from 'angular-oauth2-oidc';
import { App } from './app';
import { provideTranslocoForTests } from './testing/transloco-testing';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideOAuthClient(),
        provideTranslocoForTests(),
      ],
    }).compileComponents();
  });

  it('renders the shell with the synthetic-data banner and a sign-in button when signed out', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;

    expect(el.querySelector('[data-testid="synthetic-banner"]')?.textContent).toContain(
      'Synthetic data',
    );
    expect(el.querySelector('[data-testid="sign-in"]')).not.toBeNull();
    expect(el.querySelector('[data-testid="user-menu"]')).toBeNull();
  });
});
