import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { AssetDetail as AssetDetailModel, assetsApi, nominalBand, Sensor } from './assets.api';

/** One asset with its sensor mappings and the MQTT topic each one is read from (read-only, ADR-004). */
@Component({
  selector: 'sf-asset-detail',
  imports: [
    MatCardModule,
    MatIconModule,
    MatProgressBarModule,
    MatTableModule,
    RouterLink,
    TranslocoPipe,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './asset-detail.html',
  styleUrl: './assets.scss',
})
export class AssetDetail {
  /** Route parameter `:id`, bound by withComponentInputBinding. */
  readonly id = input.required<string>();

  protected readonly columns = ['metric', 'unit', 'interval', 'nominal', 'topic'];

  protected readonly asset = httpResource<AssetDetailModel>(() => assetsApi.asset(this.id()));

  protected band(sensor: Sensor): string {
    return nominalBand(sensor);
  }

  /** 404 means "not in your tenant" as much as "does not exist" (ADR-008); the page says only "not found". */
  protected notFound(): boolean {
    const error = this.asset.error() as { status?: number } | undefined;
    return error?.status === 404;
  }
}
