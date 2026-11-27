import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { TranslocoPipe } from '@jsverse/transloco';
import { catchError, map, of, startWith } from 'rxjs';
import { MeService } from '../../core/api/me.service';
import { AuthService } from '../../core/auth/auth.service';

type MeState =
  | { readonly kind: 'loading' }
  | {
      readonly kind: 'ok';
      readonly subject: string | null;
      readonly username: string | null;
      readonly tenantId: string | null;
      readonly roles: readonly string[];
    }
  | { readonly kind: 'error' };

/** Week 1 landing page: proves the OIDC login and the bearer token round-trip to the Platform API. */
@Component({
  selector: 'sf-home',
  imports: [MatCardModule, MatChipsModule, MatIconModule, MatListModule, TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {
  protected readonly auth = inject(AuthService);
  private readonly meService = inject(MeService);

  protected readonly me = toSignal<MeState>(
    this.meService.me().pipe(
      map((info): MeState => ({ kind: 'ok', ...info })),
      catchError(() => of<MeState>({ kind: 'error' })),
      startWith<MeState>({ kind: 'loading' }),
    ),
    { requireSync: true },
  );

  protected readonly expiresAt = this.auth.accessTokenExpiresAt();
}
