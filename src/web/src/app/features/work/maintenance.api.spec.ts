import { HttpErrorResponse } from '@angular/common/http';
import {
  checklistProgress,
  maintenanceApi,
  sortForTechnician,
  WorkOrderSummary,
} from './maintenance.api';
import { describe as describeError } from './work-order';

const summary = (
  code: string,
  status: WorkOrderSummary['status'],
  scheduledFor: string | null,
): WorkOrderSummary => ({
  id: code,
  assetId: 'a',
  assetCode: code,
  title: code,
  priority: 'High',
  status,
  assignedTo: 'tech.one',
  scheduledFor,
  dueDate: null,
  revision: 1,
  updatedAtUtc: '2026-11-02T10:00:00Z',
});

describe('maintenance.api', () => {
  it('puts running work first, then scheduled by date, then on hold, then the rest', () => {
    const sorted = sortForTechnician([
      summary('CLOSED', 'Closed', '2026-11-01'),
      summary('HOLD', 'OnHold', '2026-11-01'),
      summary('LATER', 'Scheduled', '2026-11-09'),
      summary('SOON', 'Scheduled', '2026-11-03'),
      summary('RUN', 'InProgress', '2026-11-05'),
    ]);

    expect(sorted.map((w) => w.assetCode)).toEqual(['RUN', 'SOON', 'LATER', 'HOLD', 'CLOSED']);
  });

  it('counts resolved checklist items', () => {
    expect(
      checklistProgress([
        { id: '1', position: 1, text: 'a', status: 'Done', note: null },
        { id: '2', position: 2, text: 'b', status: 'Pending', note: null },
        { id: '3', position: 3, text: 'c', status: 'NotApplicable', note: null },
      ]),
    ).toEqual({ done: 2, total: 3 });
  });

  it('builds action URLs from the action name', () => {
    expect(maintenanceApi.action('w1', 'Complete')).toMatch(
      /\/api\/maintenance\/work-orders\/w1\/complete$/,
    );
  });

  it('turns API failures into messages a technician can act on', () => {
    const conflict = new HttpErrorResponse({ status: 409, error: { title: 'stale-version' } });
    const rule = new HttpErrorResponse({
      status: 409,
      error: { title: 'checklist-incomplete', detail: 'Finish the checklist.' },
    });

    expect(describeError(conflict)).toEqual({ key: 'work.errors.stale' });
    expect(describeError(new HttpErrorResponse({ status: 415 }))).toEqual({
      key: 'work.errors.fileType',
    });
    expect(describeError(rule)).toEqual({ text: 'Finish the checklist.' });
    expect(describeError(new Error('boom'))).toEqual({ key: 'work.errors.generic' });
  });
});
