import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './core/auth/auth.service';
import { Shell } from './shell/shell';

@Component({
  selector: 'sf-root',
  imports: [Shell],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<sf-shell />',
})
export class App {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  constructor() {
    // After the OIDC redirect lands on "/", go back to where the user wanted to be.
    effect(() => {
      if (this.auth.ready() && this.auth.isAuthenticated()) {
        const returnTo = this.auth.consumeReturnTo();
        if (returnTo && returnTo !== '/' && !returnTo.startsWith('/login')) {
          void this.router.navigateByUrl(returnTo);
        }
      }
    });
  }
}
