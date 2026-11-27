import { environment } from '../../../environments/environment';

// Mirrors the Assets DTOs in contracts/openapi/platform-api.v1.json. Change both together.

export type Criticality = 'Low' | 'Medium' | 'High';
export type AssetStatus = 'InService' | 'OutOfService' | 'Decommissioned';

export const CRITICALITIES: readonly Criticality[] = ['High', 'Medium', 'Low'];

export interface Line {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly assetCount: number;
}

export interface Plant {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly timeZone: string;
  readonly lines: readonly Line[];
}

export interface AssetSummary {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly assetClass: string;
  readonly criticality: Criticality;
  readonly status: AssetStatus;
  readonly lineId: string;
  readonly lineCode: string;
  readonly lineName: string;
  readonly plantCode: string;
  readonly sensorCount: number;
}

export interface Sensor {
  readonly id: string;
  readonly metric: string;
  readonly unit: string;
  readonly intervalSeconds: number;
  readonly nominalMin: number | null;
  readonly nominalMax: number | null;
  readonly kind: string | null;
  readonly sourceTopic: string;
}

export interface AssetDetail extends Omit<AssetSummary, 'sensorCount'> {
  readonly description: string | null;
  readonly ratedPowerKw: number | null;
  readonly version: number;
  readonly updatedAtUtc: string;
  readonly sensors: readonly Sensor[];
}

export interface AssetFilter {
  readonly lineId: string;
  readonly criticality: Criticality | '';
  readonly search: string;
}

const base = `${environment.apiBaseUrl}/api/assets`;

export const assetsApi = {
  plants: () => `${base}/plants`,
  assets: () => `${base}/assets`,
  asset: (id: string) => `${base}/assets/${encodeURIComponent(id)}`,
} as const;

/** Query parameters for GET /api/assets/assets; empty filters are left out. */
export function toQueryParams(filter: AssetFilter): Record<string, string> {
  const params: Record<string, string> = {};
  if (filter.lineId) params['lineId'] = filter.lineId;
  if (filter.criticality) params['criticality'] = filter.criticality;
  const search = filter.search.trim();
  if (search) params['search'] = search;
  return params;
}

/** "20–80 °C", "≥ 5 bar", "≤ 3 mm/s" or "—". */
export function nominalBand(sensor: Pick<Sensor, 'nominalMin' | 'nominalMax' | 'unit'>): string {
  const { nominalMin: min, nominalMax: max, unit } = sensor;
  if (min !== null && max !== null) return `${min}–${max} ${unit}`;
  if (min !== null) return `≥ ${min} ${unit}`;
  if (max !== null) return `≤ ${max} ${unit}`;
  return '—';
}
