import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslocoForTests } from '../../testing/transloco-testing';
import { AssetDetail } from './asset-detail';
import { AssetDetail as AssetDetailModel, assetsApi } from './assets.api';

const detail: AssetDetailModel = {
  id: 'a1',
  code: 'MTR-042',
  name: 'Filler drive motor',
  assetClass: 'MOTOR',
  criticality: 'High',
  status: 'InService',
  description: null,
  ratedPowerKw: 15,
  lineId: 'la',
  lineCode: 'line-a',
  lineName: 'Line A',
  plantCode: 'plant-01',
  version: 7,
  updatedAtUtc: '2026-11-02T12:00:00Z',
  sensors: [
    {
      id: 's1',
      metric: 'temperature',
      unit: '°C',
      intervalSeconds: 5,
      nominalMin: 40,
      nominalMax: 70,
      kind: 'analog',
      sourceTopic: 'plant/tnt-alsafa/plant-01/line-a/MTR-042/temperature',
    },
  ],
};

describe('AssetDetail', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AssetDetail],
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

  function render(id: string) {
    const fixture = TestBed.createComponent(AssetDetail);
    fixture.componentRef.setInput('id', id);
    // Why tick, not whenStable: whenStable waits for the pending request that the test has yet to flush.
    fixture.detectChanges();
    TestBed.tick();
    return fixture;
  }

  it('shows the asset, its sensors with the nominal band and the MQTT topic', async () => {
    const fixture = render('a1');
    http.expectOne(assetsApi.asset('a1')).flush(detail);
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;

    expect(el.querySelector('h1')?.textContent).toContain('MTR-042');
    expect(el.querySelector('[data-testid="asset-facts"]')?.textContent).toContain('15 kW');
    const row = el.querySelector('[data-testid="sensor-row"]');
    expect(row?.textContent).toContain('40–70 °C');
    expect(row?.textContent).toContain('plant/tnt-alsafa/plant-01/line-a/MTR-042/temperature');
    expect(el.querySelector('[data-testid="synthetic-tag"]')).not.toBeNull();
  });

  it('says "not found" for a 404, which is also what another tenant\'s asset returns', async () => {
    const fixture = render('foreign');
    http
      .expectOne(assetsApi.asset('foreign'))
      .flush(null, { status: 404, statusText: 'Not Found' });
    await fixture.whenStable();

    expect(
      (fixture.nativeElement as HTMLElement).querySelector('[data-testid="asset-error"]')
        ?.textContent,
    ).toContain('Asset not found.');
  });
});
