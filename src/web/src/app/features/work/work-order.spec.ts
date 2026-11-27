import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslocoForTests } from '../../testing/transloco-testing';
import { maintenanceApi, WorkOrderDetail } from './maintenance.api';
import { WorkOrder } from './work-order';

function detail(overrides: Partial<WorkOrderDetail> = {}): WorkOrderDetail {
  return {
    id: 'w1',
    assetId: 'a1',
    assetCode: 'MTR-042',
    title: 'Bearing noise',
    description: 'Vibration above alarm band',
    priority: 'High',
    status: 'Scheduled',
    statusReason: null,
    assignedTo: 'tech.one',
    scheduledFor: '2026-11-04',
    dueDate: '2026-11-10',
    approvedBy: 'manager',
    disposition: null,
    revision: 3,
    version: 41,
    createdAtUtc: '2026-11-02T10:00:00Z',
    updatedAtUtc: '2026-11-02T10:00:00Z',
    request: {
      id: 'r1',
      reason: 'Vibration',
      requestedBy: 'planner',
      requestedAtUtc: '2026-11-02T10:00:00Z',
      decision: 'Approved',
    },
    checklist: [
      { id: 'c1', position: 1, text: 'Check bearing play', status: 'Pending', note: null },
      { id: 'c2', position: 2, text: 'Measure vibration', status: 'Pending', note: null },
    ],
    history: [
      {
        revision: 1,
        action: 'Requested',
        fromStatus: 'Requested',
        toStatus: 'Requested',
        actor: 'planner',
        note: null,
        atUtc: '2026-11-02T10:00:00Z',
      },
    ],
    findings: [],
    availableActions: ['Start', 'Hold'],
    ...overrides,
  };
}

describe('WorkOrder (technician page)', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [WorkOrder],
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

  async function render(initial: WorkOrderDetail) {
    const fixture = TestBed.createComponent(WorkOrder);
    fixture.componentRef.setInput('id', initial.id);
    fixture.detectChanges();
    TestBed.tick();
    http.expectOne(maintenanceApi.workOrder(initial.id)).flush(initial);
    await fixture.whenStable();
    return { fixture, el: fixture.nativeElement as HTMLElement };
  }

  function click(el: HTMLElement, testId: string) {
    (el.querySelector(`[data-testid="${testId}"]`) as HTMLButtonElement).click();
  }

  it('shows only the actions the server offers and sends the version it read', async () => {
    const { fixture, el } = await render(detail());

    expect(el.querySelector('[data-testid="action-Start"]')).not.toBeNull();
    expect(el.querySelector('[data-testid="action-Complete"]')).toBeNull();
    expect(el.querySelector('[data-testid="finding-form"]')).toBeNull();

    click(el, 'action-Start');
    const req = http.expectOne(maintenanceApi.action('w1', 'Start'));
    expect(req.request.body).toEqual({ version: 41 });
    req.flush(
      detail({ status: 'InProgress', version: 42, availableActions: ['Hold', 'Complete'] }),
    );
    await fixture.whenStable();

    expect(el.querySelector('[data-testid="wo-summary"]')?.textContent).toContain('In progress');
    expect(el.querySelectorAll('[data-testid="outcome-Done"]')).toHaveLength(2);
    expect(
      (el.querySelector('[data-testid="action-Complete"]') as HTMLButtonElement).disabled,
    ).toBe(true);
  });

  it('records checklist outcomes and enables Complete once every item has one', async () => {
    const running = detail({ status: 'InProgress', availableActions: ['Hold', 'Complete'] });
    const { fixture, el } = await render(running);

    (el.querySelectorAll('[data-testid="outcome-Done"]')[0] as HTMLButtonElement).click();
    const put = http.expectOne(maintenanceApi.checklistItem('w1', 'c1'));
    expect(put.request.body).toEqual({ version: 41, status: 'Done', note: null });
    put.flush({
      ...running,
      version: 43,
      checklist: running.checklist.map((c) => ({ ...c, status: 'Done' as const })),
    });
    await fixture.whenStable();

    expect(
      (el.querySelector('[data-testid="action-Complete"]') as HTMLButtonElement).disabled,
    ).toBe(false);
  });

  it('asks for a reason before putting work on hold', async () => {
    const { fixture, el } = await render(detail());

    click(el, 'action-Hold');
    await fixture.whenStable();
    expect((el.querySelector('[data-testid="confirm-hold"]') as HTMLButtonElement).disabled).toBe(
      true,
    );

    const input = el.querySelector('[data-testid="hold-reason"]') as HTMLInputElement;
    input.value = 'Waiting for bearing kit';
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    click(el, 'confirm-hold');

    const req = http.expectOne(maintenanceApi.action('w1', 'Hold'));
    expect(req.request.body).toEqual({ version: 41, reason: 'Waiting for bearing kit' });
    req.flush(
      detail({
        status: 'OnHold',
        statusReason: 'Waiting for bearing kit',
        availableActions: ['Resume'],
      }),
    );
    await fixture.whenStable();
    expect(el.textContent).toContain('Waiting for bearing kit');
  });

  it('reloads and explains when someone else changed the work order', async () => {
    const { fixture, el } = await render(detail());

    click(el, 'action-Start');
    http
      .expectOne(maintenanceApi.action('w1', 'Start'))
      .flush({ title: 'stale-version' }, { status: 409, statusText: 'Conflict' });
    TestBed.tick();
    http.expectOne(maintenanceApi.workOrder('w1')).flush(detail({ version: 50, revision: 4 }));
    await fixture.whenStable();

    expect(el.querySelector('[data-testid="wo-message"]')?.textContent).toContain(
      'Someone else changed',
    );
    expect(el.textContent).toContain('Revision 4');
  });

  it('refuses files over 5 MB before uploading', async () => {
    const finding = {
      id: 'f1',
      technician: 'tech.one',
      condition: 'Worn' as const,
      notes: 'Play confirmed',
      recordedAtUtc: '2026-11-02T11:00:00Z',
      attachments: [],
    };
    const { fixture, el } = await render(
      detail({ status: 'InProgress', availableActions: ['Hold', 'Complete'], findings: [finding] }),
    );

    const input = el.querySelector('[data-testid="file-input"]') as HTMLInputElement;
    const big = new File([new Uint8Array(5 * 1024 * 1024 + 1)], 'huge.jpg', { type: 'image/jpeg' });
    Object.defineProperty(input, 'files', { value: [big] });
    input.dispatchEvent(new Event('change'));
    await fixture.whenStable();

    expect(el.querySelector('[data-testid="wo-message"]')?.textContent).toContain(
      'larger than 5 MB',
    );
  });
});
