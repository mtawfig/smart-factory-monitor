import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { debounceTime, distinctUntilChanged, map } from 'rxjs';
import {
  AssetFilter,
  AssetSummary,
  assetsApi,
  CRITICALITIES,
  Criticality,
  Plant,
  toQueryParams,
} from './assets.api';

/** Asset explorer: the tenant's machines, filtered by line, criticality and free text. */
@Component({
  selector: 'sf-asset-list',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule,
    MatTableModule,
    TranslocoPipe,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './asset-list.html',
  styleUrl: './assets.scss',
})
export class AssetList {
  protected readonly criticalities = CRITICALITIES;
  protected readonly columns = [
    'code',
    'name',
    'assetClass',
    'line',
    'criticality',
    'status',
    'sensorCount',
  ];

  protected readonly filterForm = inject(NonNullableFormBuilder).group({
    lineId: '',
    criticality: '' as Criticality | '',
    search: '',
  });

  // Why debounce: one request per pause in typing, not one per keystroke.
  private readonly filter = toSignal(
    this.filterForm.valueChanges.pipe(
      debounceTime(250),
      map(() => this.filterForm.getRawValue()),
      distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b)),
    ),
    { initialValue: this.filterForm.getRawValue() as AssetFilter },
  );

  protected readonly plants = httpResource<Plant[]>(() => assetsApi.plants(), { defaultValue: [] });

  protected readonly lines = computed(() =>
    this.plants.value().flatMap((p) => p.lines.map((l) => ({ ...l, plantName: p.name }))),
  );

  protected readonly assets = httpResource<AssetSummary[]>(
    () => ({ url: assetsApi.assets(), params: toQueryParams(this.filter()) }),
    { defaultValue: [] },
  );

  protected readonly hasFilter = computed(
    () => Object.keys(toQueryParams(this.filter())).length > 0,
  );

  protected clearFilters(): void {
    this.filterForm.reset();
  }
}
