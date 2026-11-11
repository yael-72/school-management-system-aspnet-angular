import { Component, computed, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { toErrorMessage } from '../../core/api/api-error';
import { Card } from '../../shared/components/card/card';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { Skeleton } from '../../shared/components/skeleton/skeleton';
import { toPositiveId } from '../../shared/utils/route-params';
import { ClassesService } from '../classes/classes.service';
import { AttendanceRegister } from './attendance-register';
import { AttendanceReport } from './attendance-report';

type AttendanceView = 'register' | 'report';

/**
 * /attendance – pick a class, then either mark today's attendance or view a report.
 * The class is kept in the URL (?classId=3) so the page can be bookmarked or linked to.
 */
@Component({
  selector: 'app-attendance-page',
  imports: [AttendanceRegister, AttendanceReport, Card, EmptyState, Skeleton],
  templateUrl: './attendance-page.html',
  styleUrl: './attendance-page.scss',
})
export class AttendancePage {
  private readonly classesService = inject(ClassesService);
  private readonly router = inject(Router);

  readonly classId = input(null, { transform: toPositiveId });

  protected readonly view = signal<AttendanceView>('register');

  /** For a teacher the API returns only the classes she teaches in. */
  protected readonly classes = rxResource({
    stream: () => this.classesService.getClasses({ pageSize: 100 }),
  });

  /** The class from the URL, or the first class in the list when none was chosen yet. */
  protected readonly selectedClassId = computed(
    () => this.classId() ?? this.classes.value()?.items[0]?.id ?? null,
  );

  protected readonly classesError = computed(() =>
    this.classes.error() ? toErrorMessage(this.classes.error()) : null,
  );

  protected selectClass(value: string): void {
    void this.router.navigate([], { queryParams: { classId: toPositiveId(value) } });
  }
}
