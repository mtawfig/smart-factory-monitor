import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslocoForTests } from '../../testing/transloco-testing';
import { maintenanceApi, WorkOrderSummary } from './maintenance.api';
import { MyWork } from './my-work';

const job = (id: string, status: WorkOrderSummary['status']): WorkOrderSummary => ({
  id,
  assetId: 'a',
  assetCode: 'MTR-042',
  title: `Job ${id}`,
  priority: 'High',
  status,
  assignedTo: 'tech.one',
  scheduledFor: '2026-11-04',
  dueDate: null,
  revision: 3,
  updatedAtUtc: '2026-11-02T10:00:00Z',
});

describe('MyWork', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [MyWork],
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

  async function render(jobs: WorkOrderSummary[]) {
    const fixture = TestBed.createComponent(MyWork);
    // Why tick, not whenStable: whenStable waits for the pending request that the test has yet to flush.
    fixture.detectChanges();
    TestBed.tick();
    http.expectOne(maintenanceApi.workOrders()).flush(jobs);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('lists open jobs as cards linking to the job, with the synthetic-data label', async () => {
    const el = await render([job('w1', 'Scheduled'), job('w2', 'Closed'), job('w3', 'InProgress')]);

    const cards = el.querySelectorAll('[data-testid="open-work"] [data-testid="work-card"]');
    expect(cards).toHaveLength(2);
    expect(cards[0].textContent).toContain('In progress');
    expect(cards[0].getAttribute('href')).toBe('/my-work/w3');
    expect(el.textContent).toContain('Recently finished');
    expect(el.querySelector('[data-testid="synthetic-tag"]')?.textContent).toContain(
      'Synthetic data',
    );
  });

  it('says so when nothing is assigned', async () => {
    const el = await render([]);

    expect(el.querySelector('[data-testid="no-work"]')?.textContent).toContain('Nothing assigned');
  });
});
