import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'sf-login',
  imports: [MatCardModule, MatButtonModule, MatIconModule, TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <mat-card class="card" appearance="outlined">
      <mat-card-header>
        <mat-card-title>{{ 'auth.loginTitle' | transloco }}</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        <p>{{ 'auth.loginBody' | transloco }}</p>
      </mat-card-content>
      <mat-card-actions align="end">
        <button mat-flat-button (click)="signIn()" data-testid="login-button">
          <mat-icon>login</mat-icon>
          {{ 'auth.signIn' | transloco }}
        </button>
      </mat-card-actions>
    </mat-card>
  `,
  styles: `
    .card {
      max-width: 520px;
      margin: 3rem auto;
    }
  `,
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  protected signIn(): void {
    const returnTo = this.route.snapshot.queryParamMap.get('returnTo') ?? '/';
    this.auth.login(returnTo);
  }
}
