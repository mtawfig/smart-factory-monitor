import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

/** Mirrors CallerInfo in the Platform API (BuildingBlocks/Auth). */
export interface CallerInfo {
  readonly subject: string | null;
  readonly username: string | null;
  readonly tenantId: string | null;
  readonly roles: readonly string[];
}

@Injectable({ providedIn: 'root' })
export class MeService {
  private readonly http = inject(HttpClient);

  /** GET /api/me — the API's view of the caller, from the validated bearer token. */
  me(): Observable<CallerInfo> {
    return this.http.get<CallerInfo>(`${environment.apiBaseUrl}/api/me`);
  }
}
