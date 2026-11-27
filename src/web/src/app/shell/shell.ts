import { BreakpointObserver } from '@angular/cdk/layout';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { map } from 'rxjs';
import { AuthService } from '../core/auth/auth.service';
import { PersonaRole } from '../core/auth/claims';
import { SyntheticBanner } from './synthetic-banner';

interface NavItem {
  readonly key: string;
  readonly icon: string;
  readonly route: string | null;
  readonly week?: number;
  /** Shown only to these roles; omitted means everyone. The API enforces access either way. */
  readonly roles?: readonly PersonaRole[];
}

/** Below this width the navigation becomes an overlay, so a 360 px phone keeps its whole width for content. */
export const NARROW_LAYOUT = '(max-width: 720px)';

/** Application frame (ADR-006): desktop-first, with an overlay navigation on phones for the technician pages. */
@Component({
  selector: 'sf-shell',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    MatTooltipModule,
    TranslocoPipe,
    SyntheticBanner,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class Shell {
  protected readonly auth = inject(AuthService);

  protected readonly narrow = toSignal(
    inject(BreakpointObserver)
      .observe(NARROW_LAYOUT)
      .pipe(map((state) => state.matches)),
    { initialValue: false },
  );

  private readonly navItems: readonly NavItem[] = [
    { key: 'nav.home', icon: 'dashboard', route: '/' },
    { key: 'nav.myWork', icon: 'assignment', route: '/my-work', roles: ['technician'] },
    { key: 'nav.assets', icon: 'precision_manufacturing', route: '/assets' },
    { key: 'nav.maintenance', icon: 'build', route: null, week: 4 },
    { key: 'nav.risk', icon: 'monitor_heart', route: null, week: 8 },
  ];

  protected readonly visibleNav = computed(() => {
    const roles = this.auth.roles();
    return this.navItems.filter((item) => !item.roles || item.roles.some((r) => roles.includes(r)));
  });
}
