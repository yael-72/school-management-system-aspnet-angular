import { Component, computed, inject, signal } from '@angular/core';
import { rxResource, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { debounceTime } from 'rxjs';
import { toErrorMessage } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { Dialog } from '../../shared/components/dialog/dialog';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { Paginator } from '../../shared/components/paginator/paginator';
import { Skeleton } from '../../shared/components/skeleton/skeleton';
import { ClassCard } from './class-card';
import { ClassFormDialog } from './class-form-dialog';
import { ClassSummary, GRADE_LEVEL_NAMES } from './classes.models';
import { ClassesService } from './classes.service';

/** The grades this school teaches (junior high + high school). One tab per grade. */
const SCHOOL_GRADE_LEVELS = [7, 8, 9, 10, 11, 12];
const PAGE_SIZE = 12;

@Component({
  selector: 'app-classes-page',
  imports: [ClassCard, ClassFormDialog, Dialog, EmptyState, Paginator, Skeleton],
  templateUrl: './classes-page.html',
  styleUrl: './classes-page.scss',
})
export class ClassesPage {
  private readonly classesService = inject(ClassesService);
  private readonly authService = inject(AuthService);

  protected readonly isAdmin = computed(() => this.authService.hasRole('Admin'));

  protected readonly gradeTabs = SCHOOL_GRADE_LEVELS.map((value) => ({
    value,
    name: GRADE_LEVEL_NAMES[value],
  }));
  protected readonly skeletonCards = Array.from({ length: 6 }, (_, i) => i);

  // Filter state. Changing any of these reloads the list automatically (see `classes` below).
  protected readonly gradeLevel = signal<number | null>(null);
  protected readonly page = signal(1);
  protected readonly searchText = signal('');
  /** The search text, but only after the user stopped typing for 300ms, so we don't call the API on every key. */
  private readonly search = toSignal(toObservable(this.searchText).pipe(debounceTime(300)), {
    initialValue: '',
  });

  protected readonly classes = rxResource({
    params: () => ({
      gradeLevel: this.gradeLevel() ?? undefined,
      search: this.search(),
      page: this.page(),
      pageSize: PAGE_SIZE,
    }),
    stream: ({ params }) => this.classesService.getClasses(params),
  });

  // Dialog state (admin only).
  protected readonly formOpen = signal(false);
  protected readonly editedClass = signal<ClassSummary | null>(null);
  protected readonly classToDelete = signal<ClassSummary | null>(null);
  protected readonly deleting = signal(false);
  protected readonly deleteError = signal<string | null>(null);

  protected readonly errorMessage = computed(() =>
    this.classes.error() ? toErrorMessage(this.classes.error()) : null,
  );

  protected selectGrade(gradeLevel: number | null): void {
    this.gradeLevel.set(gradeLevel);
    this.page.set(1);
  }

  protected onSearch(text: string): void {
    this.searchText.set(text);
    this.page.set(1);
  }

  protected openCreate(): void {
    this.editedClass.set(null);
    this.formOpen.set(true);
  }

  protected openEdit(schoolClass: ClassSummary): void {
    this.editedClass.set(schoolClass);
    this.formOpen.set(true);
  }

  protected onSaved(): void {
    this.formOpen.set(false);
    this.classes.reload();
  }

  protected askDelete(schoolClass: ClassSummary): void {
    this.deleteError.set(null);
    this.classToDelete.set(schoolClass);
  }

  protected confirmDelete(): void {
    const schoolClass = this.classToDelete();
    if (!schoolClass) {
      return;
    }

    this.deleting.set(true);
    this.classesService.deleteClass(schoolClass.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.classToDelete.set(null);
        this.classes.reload();
      },
      error: (error: unknown) => {
        this.deleting.set(false);
        this.deleteError.set(toErrorMessage(error));
      },
    });
  }
}
