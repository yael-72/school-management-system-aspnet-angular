import { Component, computed, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { toErrorMessage } from '../../core/api/api-error';
import { Avatar } from '../../shared/components/avatar/avatar';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { ScoreBadge } from '../../shared/components/score-badge/score-badge';
import { Skeleton } from '../../shared/components/skeleton/skeleton';
import { startOfMonthIsoDate, todayIsoDate } from '../../shared/utils/dates';
import { AttendanceService } from './attendance.service';

/** Totals of each status per student over a date range (default: this month so far). */
@Component({
  selector: 'app-attendance-report',
  imports: [Avatar, EmptyState, ScoreBadge, Skeleton],
  templateUrl: './attendance-report.html',
  styleUrl: './attendance-report.scss',
})
export class AttendanceReport {
  private readonly attendanceService = inject(AttendanceService);

  readonly classId = input.required<number>();

  protected readonly today = todayIsoDate();
  protected readonly from = signal(startOfMonthIsoDate());
  protected readonly to = signal(this.today);
  protected readonly skeletonRows = Array.from({ length: 8 }, (_, i) => i);

  /** ISO dates compare correctly as strings ("2026-10-31" < "2026-11-01"). */
  protected readonly rangeIsValid = computed(() => this.from() <= this.to());

  protected readonly report = rxResource({
    // Returning undefined keeps the resource idle, so an invalid range never reaches the API.
    params: () =>
      this.rangeIsValid()
        ? { classId: this.classId(), from: this.from(), to: this.to() }
        : undefined,
    stream: ({ params }) =>
      this.attendanceService.getReport(params.classId, params.from, params.to),
  });

  protected readonly errorMessage = computed(() =>
    this.report.error() ? toErrorMessage(this.report.error()) : null,
  );
}
