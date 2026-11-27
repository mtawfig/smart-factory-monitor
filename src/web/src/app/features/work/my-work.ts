import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import {
  isOpenForTechnician,
  maintenanceApi,
  sortForTechnician,
  WorkOrderSummary,
} from './maintenance.api';

/**
 * The technician's job list. The API returns only work assigned to the caller, so there is no filter to forget.
 * Cards, not a table: it must read well on a 360 px phone.
 */
@Component({
  selector: 'sf-my-work',
  imports: [MatIconModule, MatProgressBarModule, RouterLink, TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './my-work.html',
  styleUrl: './work.scss',
})
export class MyWork {
  protected readonly list = httpResource<WorkOrderSummary[]>(() => maintenanceApi.workOrders(), {
    defaultValue: [],
  });

  protected readonly open = computed(() =>
    sortForTechnician(this.list.value().filter((w) => isOpenForTechnician(w.status))),
  );

  protected readonly done = computed(() =>
    this.list
      .value()
      .filter((w) => !isOpenForTechnician(w.status))
      .slice(0, 10),
  );
}
