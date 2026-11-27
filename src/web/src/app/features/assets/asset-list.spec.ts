import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslocoForTests } from '../../testing/transloco-testing';
import { AssetList } from './asset-list';
import { AssetSummary, assetsApi, Plant } from './assets.api';

const plants: Plant[] = [
  {
    id: 'p1',
    code: 'plant-01',
    name: 'Plant 01',
    timeZone: 'Asia/Dubai',
    lines: [
      { id: 'la', code: 'line-a', name: 'Line A', assetCount: 1 },
      { id: 'lb', code: 'line-b', name: 'Line B', assetCount: 1 },
    ],
  },
];

const asset = (code: string, lineId: string, lineName: string): AssetSummary => ({
  id: `id-${code}`,
  code,
  name: `Machine ${code}`,
  assetClass: 'MOTOR',
  criticality: 'High',
  status: 'InService',
  lineId,
  lineCode: lineId,
  lineName,
  plantCode: 'plant-01',
  sensorCount: 4,
});

describe('AssetList', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AssetList],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideTranslocoForTests(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  function render() {
    const fixture = TestBed.createComponent(AssetList);
    // Why tick, not whenStable: whenStable waits for the pending requests that the test has yet to flush.
    fixture.detectChanges();
    TestBed.tick();
    http.expectOne(assetsApi.plants()).flush(plants);
    return fixture;
  }

  it('lists the assets with a link to each detail page and the synthetic-data label', async () => {
    const fixture = render();
    http
      .expectOne((r) => r.url === assetsApi.assets() && r.params.keys().length === 0)
      .flush([asset('MTR-042', 'la', 'Line A'), asset('CNC-018', 'lb', 'Line B')]);
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;

    const rows = el.querySelectorAll('[data-testid="asset-row"]');
    expect(rows).toHaveLength(2);
    expect(rows[0].textContent).toContain('MTR-042');
    expect(rows[0].textContent).toContain('In service');
    expect(rows[0].querySelector('a')?.getAttribute('href')).toBe('/assets/id-MTR-042');
    expect(el.querySelector('[data-testid="synthetic-tag"]')?.textContent).toContain(
      'Synthetic data',
    );
  });

  it('asks the API again with the line filter when a line is chosen', async () => {
    vi.useFakeTimers();
    try {
      const fixture = render();
      http.expectOne((r) => r.url === assetsApi.assets()).flush([]);

      fixture.componentInstance['filterForm'].controls.lineId.setValue('lb');
      await vi.advanceTimersByTimeAsync(300);
      TestBed.tick();

      const req = http.expectOne((r) => r.url === assetsApi.assets());
      expect(req.request.params.get('lineId')).toBe('lb');
      req.flush([asset('CNC-018', 'lb', 'Line B')]);
    } finally {
      vi.useRealTimers();
    }
  });

  it('shows an error instead of an empty table when the API fails', async () => {
    const fixture = render();
    http
      .expectOne((r) => r.url === assetsApi.assets())
      .flush('boom', { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;

    expect(el.querySelector('[data-testid="assets-error"]')).not.toBeNull();
    expect(el.querySelector('[data-testid="asset-table"]')).toBeNull();
  });
});
