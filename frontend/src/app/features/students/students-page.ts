import { DatePipe } from '@angular/common';
import { Component, computed, inject, input, signal } from '@angular/core';
import { rxResource, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { debounceTime } from 'rxjs';
import { SortDirection } from '../../core/api/api.models';
import { toErrorMessage } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { Avatar } from '../../shared/components/avatar/avatar';
import { Card } from '../../shared/components/card/card';
import { Dialog } from '../../shared/components/dialog/dialog';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { Paginator } from '../../shared/components/paginator/paginator';
import { ScoreBadge } from '../../shared/components/score-badge/score-badge';
import { Skeleton } from '../../shared/components/skeleton/skeleton';
import { ClassesService } from '../classes/classes.service';
import { StudentFormDialog } from './student-form-dialog';
import { StudentListItem, StudentSortField } from './students.models';
import { StudentsService } from './students.service';

const PAGE_SIZE = 15;

/** Query params are strings ("3"); turn ?classId=3 into the number 3, and a missing one into null. */
function toClassId(value: string | undefined): number | null {
  const id = Number(value);
  return Number.isInteger(id) && id > 0 ? id : null;
}

@Component({
  selector: 'app-students-page',
  imports: [
    Avatar,
    Card,
    DatePipe,
    Dialog,
    EmptyState,
    Paginator,
    RouterLink,
    ScoreBadge,
    Skeleton,
    StudentFormDialog,
  ],
  templateUrl: './students-page.html',
  styleUrl: './students-page.scss',
})
export class StudentsPage {
  private readonly studentsService = inject(StudentsService);
  private readonly classesService = inject(ClassesService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  /** From the URL (?classId=3), so a filtered list can be bookmarked or opened from a class card. */
  readonly classId = input(null, { transform: toClassId });

  protected readonly isAdmin = computed(() => this.authService.hasRole('Admin'));
  protected readonly skeletonRows = Array.from({ length: 8 }, (_, i) => i);

  // Table state. Changing any of these reloads the list (see `students` below).
  protected readonly page = signal(1);
  protected readonly sortBy = signal<StudentSortField>('lastName');
  protected readonly sortDir = signal<SortDirection>('asc');
  protected readonly searchText = signal('');
  /** The search text after the user stopped typing for 300ms, so we don't call the API on every key. */
  private readonly search = toSignal(toObservable(this.searchText).pipe(debounceTime(300)), {
    initialValue: '',
  });

  protected readonly students = rxResource({
    params: () => ({
      classId: this.classId() ?? undefined,
      search: this.search(),
      sortBy: this.sortBy(),
      sortDir: this.sortDir(),
      page: this.page(),
      pageSize: PAGE_SIZE,
    }),
    stream: ({ params }) => this.studentsService.getStudents(params),
  });

  /** Classes for the filter (for a teacher the API returns only her classes). */
  protected readonly classes = rxResource({
    stream: () => this.classesService.getClasses({ pageSize: 100 }),
  });

  protected readonly errorMessage = computed(() =>
    this.students.error() ? toErrorMessage(this.students.error()) : null,
  );

  // Dialog state (admin only).
  protected readonly formOpen = signal(false);
  protected readonly editedStudentId = signal<number | null>(null);
  protected readonly studentToDelete = signal<StudentListItem | null>(null);
  protected readonly deleting = signal(false);
  protected readonly deleteError = signal<string | null>(null);

  protected onSearch(text: string): void {
    this.searchText.set(text);
    this.page.set(1);
  }

  protected filterByClass(value: string): void {
    this.page.set(1);
    // Updating the URL updates the classId input, which reloads the list.
    void this.router.navigate([], {
      queryParams: { classId: toClassId(value) },
      queryParamsHandling: 'merge',
    });
  }

  /** Click on a column header: sort by it, or flip the direction if it is already the sorted column. */
  protected sort(field: StudentSortField): void {
    if (this.sortBy() === field) {
      this.sortDir.update((dir) => (dir === 'asc' ? 'desc' : 'asc'));
    } else {
      this.sortBy.set(field);
      this.sortDir.set('asc');
    }
    this.page.set(1);
  }

  protected sortIcon(field: StudentSortField): string {
    if (this.sortBy() !== field) {
      return 'unfold_more';
    }
    return this.sortDir() === 'asc' ? 'arrow_upward' : 'arrow_downward';
  }

  protected ariaSort(field: StudentSortField): 'ascending' | 'descending' | null {
    if (this.sortBy() !== field) {
      return null;
    }
    return this.sortDir() === 'asc' ? 'ascending' : 'descending';
  }

  protected openCreate(): void {
    this.editedStudentId.set(null);
    this.formOpen.set(true);
  }

  protected openEdit(student: StudentListItem): void {
    this.editedStudentId.set(student.id);
    this.formOpen.set(true);
  }

  protected onSaved(): void {
    this.formOpen.set(false);
    this.students.reload();
  }

  protected askDelete(student: StudentListItem): void {
    this.deleteError.set(null);
    this.studentToDelete.set(student);
  }

  protected confirmDelete(): void {
    const student = this.studentToDelete();
    if (!student) {
      return;
    }

    this.deleting.set(true);
    this.studentsService.deleteStudent(student.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.studentToDelete.set(null);
        this.students.reload();
      },
      error: (error: unknown) => {
        this.deleting.set(false);
        this.deleteError.set(toErrorMessage(error));
      },
    });
  }
}
