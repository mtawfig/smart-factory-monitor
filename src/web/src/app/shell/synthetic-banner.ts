import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoPipe } from '@jsverse/transloco';

/**
 * Always-visible label required by the content rules: synthetic data, advisory only.
 * It is part of the shell so no page can appear without it.
 */
@Component({
  selector: 'sf-synthetic-banner',
  imports: [MatIconModule, TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="banner" role="note" data-testid="synthetic-banner">
      <mat-icon aria-hidden="true">science</mat-icon>
      <span>{{ 'app.syntheticBanner' | transloco }}</span>
    </div>
  `,
  styles: `
    .banner {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.35rem 1rem;
      font-size: 0.8rem;
      background: var(--mat-sys-tertiary-container);
      color: var(--mat-sys-on-tertiary-container);
    }
  `,
})
export class SyntheticBanner {}
