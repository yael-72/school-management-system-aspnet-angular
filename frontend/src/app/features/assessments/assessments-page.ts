import { DatePipe } from '@angular/common';
import { Component, computed, inject, input, signal } from '@angular/core';
import { rxResource, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { debounceTime } from 'rxjs';
import { SortDirection } from '../../core/api/api.models';
import { toErrorMessage } from '../../core/api/api-error';
import { SubjectsService } from '../../core/subjects/subjects.service';
import { Card } from '../../shared/components/card/card';
import { Dialog } from '../../shared/components/dialog/dialog';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { Paginator } from '../../shared/components/paginator/paginator';
import { ScoreBadge } from '../../shared/components/score-badge/score-badge';
import { Skeleton } from '../../shared/components/skeleton/skeleton';
import { todayIsoDate } from '../../shared/utils/dates';
import { toPositiveId } from '../../shared/utils/route-params';
import { ClassesService } from '../classes/classes.service';
import { AssessmentFormDialog, EditableAssessment } from './assessment-form-dialog';
import { AssessmentTypeChip } from './assessment-type-chip';
import {
  ASSESSMENT_TYPES,
  Assessment,
  AssessmentListItem,
  AssessmentSortField,
  AssessmentType,
} from './assessments.models';
import { AssessmentsService } from './assessments.service';

const PAGE_SIZE = 15;

/**
 * /assessments – the exams, quizzes and assignments of the user's classes (Horizon "Tables" page).
 * The class filter is kept in the URL (?classId=3) so it can be linked to from other pages.
 */
@Component({
  selector: 'app-assessments-page',
  imports: [
    AssessmentFormDialog,
    AssessmentTypeChip,
    Card,
    DatePipe,
    Dialog,
    EmptyState,
    Paginator,
    RouterLink,
    ScoreBadge,
    Skeleton,
  ],
  templateUrl: './assessments-page.html',
  styleUrl: './assessments-page.scss',
})
export class AssessmentsPage {
  private readonly assessmentsService = inject(AssessmentsService);
  private readonly classesService = inject(ClassesService);
  private readonly subjectsService = inject(SubjectsService);
  private readonly router = inject(Router);

  readonly classId = input(null, { transform: toPositiveId });

  protected readonly types = ASSESSMENT_TYPES;
  protected readonly today = todayIsoDate();
  protected readonly skeletonRows = Array.from({ length: 8 }, (_, i) => i);

  // Table state. Changing any of these reloads the list.
  protected readonly page = signal(1);
  protected readonly sortBy = signal<AssessmentSortField>('date');
  protected readonly sortDir = signal<SortDirection>('desc');
  protected readonly subjectId = signal<number | null>(null);
  protected readonly type = signal<AssessmentType | null>(null);
  protected readonly searchText = signal('');
  private readonly search = toSignal(toObservable(this.searchText).pipe(debounceTime(300)), {
    initialValue: '',
  });

  protected readonly assessments = rxResource({
    params: () => ({
      classId: this.classId() ?? undefined,
      subjectId: this.subjectId() ?? undefined,
      type: this.type() ?? undefined,
      search: this.search(),
      sortBy: this.sortBy(),
      sortDir: this.sortDir(),
      page: this.page(),
      pageSize: PAGE_SIZE,
    }),
    stream: ({ params }) => this.assessmentsService.getAssessments(params),
  });

  /** For a teacher the API returns only her classes. */
  protected readonly classes = rxResource({
    stream: () => this.classesService.getClasses({ pageSize: 100 }),
  });

  protected readonly subjects = rxResource({
    stream: () => this.subjectsService.getSubjects(),
  });

  protected readonly errorMessage = computed(() =>
    this.assessments.error() ? toErrorMessage(this.assessments.error()) : null,
  );

  // Dialogs
  protected readonly formOpen = signal(false);
  protected readonly editedAssessment = signal<EditableAssessment | null>(null);
  protected readonly assessmentToDelete = signal<AssessmentListItem | null>(null);
  protected readonly deleting = signal(false);
  protected readonly deleteError = signal<string | null>(null);

  protected onSearch(text: string): void {
    this.searchText.set(text);
    this.page.set(1);
  }

  protected filterByClass(value: string): void {
    this.page.set(1);
    void this.router.navigate([], {
      queryParams: { classId: toPositiveId(value) },
      queryParamsHandling: 'merge',
    });
  }

  protected filterBySubject(value: string): void {
    this.subjectId.set(toPositiveId(value));
    this.page.set(1);
  }

  protected filterByType(type: AssessmentType | null): void {
    this.type.set(type);
    this.page.set(1);
  }

  /** Click on a column header: sort by it, or flip the direction if it is already sorted by it. */
  protected sort(field: AssessmentSortField): void {
    if (this.sortBy() === field) {
      this.sortDir.update((dir) => (dir === 'asc' ? 'desc' : 'asc'));
    } else {
      this.sortBy.set(field);
      // Dates and averages are most useful from the highest down; text from A to Z.
      this.sortDir.set(field === 'date' || field === 'averageScore' ? 'desc' : 'asc');
    }
    this.page.set(1);
  }

  protected sortIcon(field: AssessmentSortField): string {
    if (this.sortBy() !== field) {
      return 'unfold_more';
    }
    return this.sortDir() === 'asc' ? 'arrow_upward' : 'arrow_downward';
  }

  protected ariaSort(field: AssessmentSortField): 'ascending' | 'descending' | null {
    if (this.sortBy() !== field) {
      return null;
    }
    return this.sortDir() === 'asc' ? 'ascending' : 'descending';
  }

  protected openCreate(): void {
    this.editedAssessment.set(null);
    this.formOpen.set(true);
  }

  protected openEdit(assessment: AssessmentListItem): void {
    this.editedAssessment.set(assessment);
    this.formOpen.set(true);
  }

  /** A new assessment opens its grade sheet; an edited one just refreshes the list. */
  protected onSaved(assessment: Assessment): void {
    const isNew = this.editedAssessment() === null;
    this.formOpen.set(false);
    if (isNew) {
      void this.router.navigate(['/assessments', assessment.id]);
    } else {
      this.assessments.reload();
    }
  }

  protected askDelete(assessment: AssessmentListItem): void {
    this.deleteError.set(null);
    this.assessmentToDelete.set(assessment);
  }

  protected confirmDelete(): void {
    const assessment = this.assessmentToDelete();
    if (!assessment) {
      return;
    }

    this.deleting.set(true);
    this.assessmentsService.deleteAssessment(assessment.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.assessmentToDelete.set(null);
        this.assessments.reload();
      },
      error: (error: unknown) => {
        this.deleting.set(false);
        this.deleteError.set(toErrorMessage(error));
      },
    });
  }
}
