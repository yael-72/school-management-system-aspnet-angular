import { DatePipe } from '@angular/common';
import { Component, computed, inject, input, linkedSignal, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { toErrorMessage } from '../../core/api/api-error';
import { Avatar } from '../../shared/components/avatar/avatar';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { Skeleton } from '../../shared/components/skeleton/skeleton';
import { toIsoDate, todayIsoDate } from '../../shared/utils/dates';
import { ATTENDANCE_STATUSES, AttendanceEntry, AttendanceStatus } from './attendance.models';
import { AttendanceService } from './attendance.service';

/** Moves an ISO date ("2026-11-10") by a number of days. */
function addDays(isoDate: string, days: number): string {
  const [year, month, day] = isoDate.split('-').map(Number);
  return toIsoDate(new Date(year, month - 1, day + days));
}

/**
 * The daily attendance register of one class: a row per student with a status picker.
 * Changes are kept locally and sent together with one "save" (PUT /api/attendance).
 */
@Component({
  selector: 'app-attendance-register',
  imports: [Avatar, DatePipe, EmptyState, Skeleton],
  templateUrl: './attendance-register.html',
  styleUrl: './attendance-register.scss',
})
export class AttendanceRegister {
  private readonly attendanceService = inject(AttendanceService);

  readonly classId = input.required<number>();

  protected readonly statuses = ATTENDANCE_STATUSES;
  protected readonly today = todayIsoDate();
  protected readonly skeletonRows = Array.from({ length: 8 }, (_, i) => i);

  protected readonly date = signal(this.today);

  protected readonly register = rxResource({
    params: () => ({ classId: this.classId(), date: this.date() }),
    stream: ({ params }) => this.attendanceService.getClassAttendance(params.classId, params.date),
  });

  /**
   * The rows the user is editing. A linkedSignal starts as a copy of the loaded register and is
   * reset automatically whenever a new register loads (another class or date, or after saving).
   */
  protected readonly rows = linkedSignal<AttendanceEntry[]>(
    () => this.register.value()?.students ?? [],
  );

  protected readonly hasChanges = computed(() => {
    const saved = this.register.value()?.students ?? [];
    return this.rows().some(
      (row, i) => row.status !== saved[i]?.status || (row.note ?? '') !== (saved[i]?.note ?? ''),
    );
  });

  /** How many students have each status, plus how many were not marked yet. */
  protected readonly counts = computed(() => {
    const counts: Record<AttendanceStatus | 'unmarked', number> = {
      Present: 0,
      Absent: 0,
      Late: 0,
      Excused: 0,
      unmarked: 0,
    };
    for (const row of this.rows()) {
      counts[row.status ?? 'unmarked']++;
    }
    return counts;
  });

  protected readonly saving = signal(false);
  protected readonly saveError = signal<string | null>(null);
  protected readonly saved = signal(false);

  protected readonly loadError = computed(() =>
    this.register.error() ? toErrorMessage(this.register.error()) : null,
  );

  protected changeDate(value: string): void {
    // An empty or future value (typed by hand) is ignored: the API rejects future dates anyway.
    if (value && value <= this.today) {
      this.date.set(value);
      this.clearMessages();
    }
  }

  protected moveDate(days: number): void {
    this.changeDate(addDays(this.date(), days));
  }

  protected setStatus(studentId: number, status: AttendanceStatus): void {
    this.updateRow(studentId, { status });
  }

  protected setNote(studentId: number, note: string): void {
    this.updateRow(studentId, { note });
  }

  /** Marks every student who has no status yet as present – the usual case in a real class. */
  protected markUnmarkedAsPresent(): void {
    this.rows.update((rows) =>
      rows.map((row) => (row.status ? row : { ...row, status: 'Present' })),
    );
    this.clearMessages();
  }

  protected save(): void {
    const records = this.rows()
      .filter((row) => row.status !== null)
      .map((row) => ({
        studentId: row.studentId,
        status: row.status as AttendanceStatus,
        note: row.note?.trim() || null,
      }));

    this.saving.set(true);
    this.clearMessages();
    this.attendanceService
      .saveClassAttendance({ classId: this.classId(), date: this.date(), records })
      .subscribe({
        next: (register) => {
          // Show what the server saved; this also resets `rows` (linkedSignal) and `hasChanges`.
          this.register.set(register);
          this.saving.set(false);
          this.saved.set(true);
        },
        error: (error: unknown) => {
          this.saving.set(false);
          this.saveError.set(toErrorMessage(error));
        },
      });
  }

  private updateRow(studentId: number, changes: Partial<AttendanceEntry>): void {
    this.rows.update((rows) =>
      rows.map((row) => (row.studentId === studentId ? { ...row, ...changes } : row)),
    );
    this.clearMessages();
  }

  private clearMessages(): void {
    this.saved.set(false);
    this.saveError.set(null);
  }
}
