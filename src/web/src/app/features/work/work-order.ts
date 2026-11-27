import { HttpClient, HttpErrorResponse, httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import {
  FormControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { Observable } from 'rxjs';
import {
  ACCEPTED_FILES,
  Attachment,
  CHECKLIST_OUTCOMES,
  ChecklistItem,
  ChecklistStatus,
  checklistProgress,
  CONDITIONS,
  ConditionLabel,
  Finding,
  MAX_FILE_BYTES,
  maintenanceApi,
  Problem,
  WorkOrderAction,
  WorkOrderDetail,
} from './maintenance.api';

/** Actions a technician takes from this page; approvals and closing belong to the planner's hub (week 4). */
const TECHNICIAN_ACTIONS: readonly WorkOrderAction[] = ['Start', 'Hold', 'Resume', 'Complete'];

/**
 * One job on a phone: what to do, the checklist, findings with photos, and the next step. The server decides which
 * actions are possible (`availableActions`); the page only shows them. Single column, large touch targets.
 */
@Component({
  selector: 'sf-work-order',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule,
    TranslocoPipe,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './work-order.html',
  styleUrl: './work.scss',
})
export class WorkOrder {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(NonNullableFormBuilder);

  /** Route parameter `:id`, bound by withComponentInputBinding. */
  readonly id = input.required<string>();

  protected readonly acceptedFiles = ACCEPTED_FILES;
  protected readonly conditions = CONDITIONS;
  protected readonly outcomes = CHECKLIST_OUTCOMES;

  protected readonly workOrder = httpResource<WorkOrderDetail>(() =>
    maintenanceApi.workOrder(this.id()),
  );
  protected readonly busy = signal(false);
  /** A translation key, or the server's own explanation of a rule violation. */
  protected readonly message = signal<{ key?: string; text?: string } | null>(null);
  protected readonly holding = signal(false);

  protected readonly actions = computed(() => {
    const available = this.workOrder.hasValue() ? this.workOrder.value().availableActions : [];
    return TECHNICIAN_ACTIONS.filter((a) => available.includes(a));
  });

  protected readonly canRecord = computed(
    () =>
      this.workOrder.hasValue() &&
      this.workOrder.value().status === 'InProgress' &&
      this.actions().includes('Complete'),
  );

  protected readonly progress = computed(() =>
    checklistProgress(this.workOrder.hasValue() ? this.workOrder.value().checklist : []),
  );

  protected readonly holdReason = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(1000)],
  });

  protected readonly findingForm = this.fb.group({
    condition: this.fb.control<ConditionLabel | ''>('', Validators.required),
    notes: this.fb.control('', [
      Validators.required,
      Validators.minLength(3),
      Validators.maxLength(4000),
    ]),
  });

  protected act(action: WorkOrderAction): void {
    if (action === 'Hold' && !this.holding()) {
      this.holding.set(true);
      return;
    }

    const w = this.workOrder.value();
    if (!w) {
      return;
    }

    const body =
      action === 'Hold'
        ? { version: w.version, reason: this.holdReason.value }
        : { version: w.version };
    this.run(
      this.http.post<WorkOrderDetail>(maintenanceApi.action(w.id, action), body),
      (updated) => {
        this.workOrder.set(updated);
        this.holding.set(false);
        this.holdReason.reset();
      },
    );
  }

  protected setItem(item: ChecklistItem, status: ChecklistStatus): void {
    const w = this.workOrder.value();
    if (!w || item.status === status) {
      return;
    }

    this.run(
      this.http.put<WorkOrderDetail>(maintenanceApi.checklistItem(w.id, item.id), {
        version: w.version,
        status,
        note: item.note,
      }),
      (updated) => this.workOrder.set(updated),
    );
  }

  protected addFinding(): void {
    const w = this.workOrder.value();
    if (!w || this.findingForm.invalid) {
      this.findingForm.markAllAsTouched();
      return;
    }

    this.run(
      this.http.post<Finding>(maintenanceApi.findings(w.id), this.findingForm.getRawValue()),
      () => {
        this.findingForm.reset();
        this.workOrder.reload();
      },
    );
  }

  protected upload(finding: Finding, event: Event): void {
    const inputEl = event.target as HTMLInputElement;
    const file = inputEl.files?.[0];
    inputEl.value = '';
    const w = this.workOrder.value();
    if (!file || !w) {
      return;
    }

    // Why check here too: a 20 MB phone photo should fail before the upload, not after it. The server still decides.
    if (file.size > MAX_FILE_BYTES) {
      this.message.set({ key: 'work.errors.tooLarge' });
      return;
    }

    const form = new FormData();
    form.append('file', file, file.name);
    this.run(this.http.post<Attachment>(maintenanceApi.attachments(w.id, finding.id), form), () =>
      this.workOrder.reload(),
    );
  }

  /** Attachments need the bearer token, so they are fetched as a blob rather than opened as a plain link. */
  protected download(attachment: Attachment): void {
    this.http.get(maintenanceApi.attachment(attachment.id), { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = attachment.fileName;
        a.click();
        setTimeout(() => URL.revokeObjectURL(url), 30_000);
      },
      error: () => this.message.set({ key: 'work.errors.download' }),
    });
  }

  protected cancelHold(): void {
    this.holding.set(false);
    this.holdReason.reset();
  }

  private run<T>(request: Observable<T>, done: (value: T) => void): void {
    this.busy.set(true);
    this.message.set(null);
    request.subscribe({
      next: (value) => {
        this.busy.set(false);
        done(value);
      },
      error: (error: unknown) => {
        this.busy.set(false);
        this.message.set(describe(error));
        if (error instanceof HttpErrorResponse && error.status === 409) {
          this.workOrder.reload();
        }
      },
    });
  }
}

/** Maps an HTTP failure to something a technician can act on. */
export function describe(error: unknown): { key?: string; text?: string } {
  if (!(error instanceof HttpErrorResponse)) {
    return { key: 'work.errors.generic' };
  }

  const problem = (error.error ?? {}) as Problem;
  if (problem.title === 'stale-version') {
    return { key: 'work.errors.stale' };
  }

  if (error.status === 415) {
    return { key: 'work.errors.fileType' };
  }

  if (error.status === 413) {
    return { key: 'work.errors.tooLarge' };
  }

  return problem.detail ? { text: problem.detail } : { key: 'work.errors.generic' };
}
