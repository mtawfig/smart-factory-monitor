import { environment } from '../../../environments/environment';

// Mirrors the Maintenance DTOs in contracts/openapi/platform-api.v1.json. Change both together.

export type WorkOrderStatus =
  | 'Requested'
  | 'Approved'
  | 'Scheduled'
  | 'InProgress'
  | 'Completed'
  | 'Closed'
  | 'Rejected'
  | 'OnHold'
  | 'Cancelled';

export type WorkOrderAction =
  'Approve' | 'Reject' | 'Schedule' | 'Start' | 'Hold' | 'Resume' | 'Complete' | 'Close' | 'Cancel';

export type ChecklistStatus = 'Pending' | 'Done' | 'NotApplicable' | 'Failed';
export type ConditionLabel = 'Good' | 'Worn' | 'Degraded' | 'Failed';

export const CONDITIONS: readonly ConditionLabel[] = ['Good', 'Worn', 'Degraded', 'Failed'];
export const CHECKLIST_OUTCOMES: readonly ChecklistStatus[] = ['Done', 'NotApplicable', 'Failed'];

/** What the server accepts, checked by magic bytes there; this list only filters the phone's file picker. */
export const ACCEPTED_FILES = 'image/jpeg,image/png,application/pdf';
export const MAX_FILE_BYTES = 5 * 1024 * 1024;

export interface WorkOrderSummary {
  readonly id: string;
  readonly assetId: string;
  readonly assetCode: string;
  readonly title: string;
  readonly priority: string;
  readonly status: WorkOrderStatus;
  readonly assignedTo: string | null;
  readonly scheduledFor: string | null;
  readonly dueDate: string | null;
  readonly revision: number;
  readonly updatedAtUtc: string;
}

export interface ChecklistItem {
  readonly id: string;
  readonly position: number;
  readonly text: string;
  readonly status: ChecklistStatus;
  readonly note: string | null;
}

export interface HistoryEntry {
  readonly revision: number;
  readonly action: string;
  readonly fromStatus: WorkOrderStatus;
  readonly toStatus: WorkOrderStatus;
  readonly actor: string;
  readonly note: string | null;
  readonly atUtc: string;
}

export interface Attachment {
  readonly id: string;
  readonly fileName: string;
  readonly contentType: string;
  readonly sizeBytes: number;
  readonly uploadedAtUtc: string;
}

export interface Finding {
  readonly id: string;
  readonly technician: string;
  readonly condition: ConditionLabel;
  readonly notes: string;
  readonly recordedAtUtc: string;
  readonly attachments: readonly Attachment[];
}

export interface WorkOrderDetail extends Omit<WorkOrderSummary, 'status'> {
  readonly status: WorkOrderStatus;
  readonly description: string | null;
  readonly statusReason: string | null;
  readonly approvedBy: string | null;
  readonly disposition: string | null;
  readonly version: number;
  readonly createdAtUtc: string;
  readonly request: {
    readonly id: string;
    readonly reason: string;
    readonly requestedBy: string;
    readonly requestedAtUtc: string;
    readonly decision: string;
  };
  readonly checklist: readonly ChecklistItem[];
  readonly history: readonly HistoryEntry[];
  readonly findings: readonly Finding[];
  readonly availableActions: readonly WorkOrderAction[];
}

/** RFC 7807 body returned for rule violations; `title` carries the stable code (e.g. `stale-version`). */
export interface Problem {
  readonly title?: string;
  readonly detail?: string;
  readonly status?: number;
}

const base = `${environment.apiBaseUrl}/api/maintenance`;

export const maintenanceApi = {
  workOrders: () => `${base}/work-orders`,
  workOrder: (id: string) => `${base}/work-orders/${encodeURIComponent(id)}`,
  action: (id: string, action: WorkOrderAction) =>
    `${base}/work-orders/${encodeURIComponent(id)}/${action.toLowerCase()}`,
  checklistItem: (id: string, itemId: string) =>
    `${base}/work-orders/${encodeURIComponent(id)}/checklist/${encodeURIComponent(itemId)}`,
  findings: (id: string) => `${base}/work-orders/${encodeURIComponent(id)}/findings`,
  attachments: (id: string, findingId: string) =>
    `${base}/work-orders/${encodeURIComponent(id)}/findings/${encodeURIComponent(findingId)}/attachments`,
  attachment: (id: string) => `${base}/attachments/${encodeURIComponent(id)}`,
} as const;

/** Statuses a technician still has to act on, in the order they matter on a phone. */
export const OPEN_FOR_TECHNICIAN: readonly WorkOrderStatus[] = [
  'InProgress',
  'Scheduled',
  'OnHold',
];

export function isOpenForTechnician(status: WorkOrderStatus): boolean {
  return OPEN_FOR_TECHNICIAN.includes(status);
}

/** Open work first (in progress, scheduled, on hold), then by scheduled date. */
export function sortForTechnician(list: readonly WorkOrderSummary[]): WorkOrderSummary[] {
  const rank = (s: WorkOrderStatus) => {
    const i = OPEN_FOR_TECHNICIAN.indexOf(s);
    return i === -1 ? OPEN_FOR_TECHNICIAN.length : i;
  };
  return [...list].sort(
    (a, b) =>
      rank(a.status) - rank(b.status) ||
      (a.scheduledFor ?? '9999').localeCompare(b.scheduledFor ?? '9999') ||
      a.assetCode.localeCompare(b.assetCode),
  );
}

export function checklistProgress(items: readonly ChecklistItem[]): {
  done: number;
  total: number;
} {
  return { done: items.filter((i) => i.status !== 'Pending').length, total: items.length };
}
