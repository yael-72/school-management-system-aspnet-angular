import { Component, computed, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { SortDirection } from '../../../core/api/api.models';
import { toErrorMessage } from '../../../core/api/api-error';
import { SubjectsService } from '../../../core/subjects/subjects.service';
import { UsersService } from '../../../core/users/users.service';
import { Avatar } from '../../../shared/components/avatar/avatar';
import { Dialog } from '../../../shared/components/dialog/dialog';
import { EmptyState } from '../../../shared/components/empty-state/empty-state';
import { Paginator } from '../../../shared/components/paginator/paginator';
import { Skeleton } from '../../../shared/components/skeleton/skeleton';
import { toPositiveId } from '../../../shared/utils/route-params';
import { ClassesService } from '../../classes/classes.service';
import { AssignmentFormDialog } from './assignment-form-dialog';
import { TeacherAssignment, TeacherAssignmentSortField } from './teacher-assignments.models';
import { TeacherAssignmentsService } from './teacher-assignments.service';

const PAGE_SIZE = 20;

/**
 * /admin/assignments – who teaches which subject in which class. The filters live in the URL
 * (?teacherId=2&classId=3), so the users table can link straight to one teacher's assignments.
 */
@Component({
  selector: 'app-assignments-admin-page',
  imports: [AssignmentFormDialog, Avatar, Dialog, EmptyState, Paginator, Skeleton],
  templateUrl: './assignments-admin-page.html',
  styleUrl: './assignments-admin-page.scss',
})
export class AssignmentsAdminPage {
  private readonly assignmentsService = inject(TeacherAssignmentsService);
  private readonly usersService = inject(UsersService);
  private readonly classesService = inject(ClassesService);
  private readonly subjectsService = inject(SubjectsService);
  private readonly router = inject(Router);

  readonly teacherId = input(null, { transform: toPositiveId });
  readonly classId = input(null, { transform: toPositiveId });
  readonly subjectId = input(null, { transform: toPositiveId });

  protected readonly skeletonRows = Array.from({ length: 6 }, (_, i) => i);
  protected readonly page = signal(1);
  protected readonly sortBy = signal<TeacherAssignmentSortField>('className');
  protected readonly sortDir = signal<SortDirection>('asc');

  protected readonly assignments = rxResource({
    params: () => ({
      teacherId: this.teacherId() ?? undefined,
      classId: this.classId() ?? undefined,
      subjectId: this.subjectId() ?? undefined,
      sortBy: this.sortBy(),
      sortDir: this.sortDir(),
      page: this.page(),
      pageSize: PAGE_SIZE,
    }),
    stream: ({ params }) => this.assignmentsService.getAssignments(params),
  });

  /** The filter lists. Teachers include inactive ones, so old assignments can still be filtered. */
  protected readonly filters = rxResource({
    stream: () =>
      forkJoin({
        teachers: this.usersService.getUsers({ pageSize: 100, sortBy: 'fullName' }),
        classes: this.classesService.getClasses({ pageSize: 100 }),
        subjects: this.subjectsService.getSubjects(),
      }),
  });

  protected readonly errorMessage = computed(() =>
    this.assignments.error() ? toErrorMessage(this.assignments.error()) : null,
  );

  protected readonly hasFilters = computed(
    () => this.teacherId() !== null || this.classId() !== null || this.subjectId() !== null,
  );

  protected readonly formOpen = signal(false);
  protected readonly formDefaults = computed(() => ({
    teacherId: this.teacherId(),
    classId: this.classId(),
    subjectId: this.subjectId(),
  }));
  protected readonly assignmentToDelete = signal<TeacherAssignment | null>(null);
  protected readonly deleting = signal(false);
  protected readonly actionError = signal<string | null>(null);

  protected filter(name: 'teacherId' | 'classId' | 'subjectId', value: string): void {
    this.page.set(1);
    void this.router.navigate([], {
      queryParams: { [name]: toPositiveId(value) },
      queryParamsHandling: 'merge',
    });
  }

  protected clearFilters(): void {
    this.page.set(1);
    void this.router.navigate([], { queryParams: {} });
  }

  protected sort(field: TeacherAssignmentSortField): void {
    if (this.sortBy() === field) {
      this.sortDir.update((dir) => (dir === 'asc' ? 'desc' : 'asc'));
    } else {
      this.sortBy.set(field);
      this.sortDir.set('asc');
    }
    this.page.set(1);
  }

  protected sortIcon(field: TeacherAssignmentSortField): string {
    if (this.sortBy() !== field) {
      return 'unfold_more';
    }
    return this.sortDir() === 'asc' ? 'arrow_upward' : 'arrow_downward';
  }

  protected ariaSort(field: TeacherAssignmentSortField): 'ascending' | 'descending' | null {
    if (this.sortBy() !== field) {
      return null;
    }
    return this.sortDir() === 'asc' ? 'ascending' : 'descending';
  }

  protected onCreated(): void {
    this.formOpen.set(false);
    this.assignments.reload();
  }

  protected askDelete(assignment: TeacherAssignment): void {
    this.actionError.set(null);
    this.assignmentToDelete.set(assignment);
  }

  protected confirmDelete(): void {
    const assignment = this.assignmentToDelete();
    if (!assignment) {
      return;
    }
    this.deleting.set(true);
    this.assignmentsService.deleteAssignment(assignment.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.assignmentToDelete.set(null);
        this.assignments.reload();
      },
      error: (error: unknown) => {
        this.deleting.set(false);
        this.assignmentToDelete.set(null);
        this.actionError.set(toErrorMessage(error));
      },
    });
  }
}
