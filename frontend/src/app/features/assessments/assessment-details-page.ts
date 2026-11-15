import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, inject, input, numberAttribute, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { toErrorMessage } from '../../core/api/api-error';
import { Card } from '../../shared/components/card/card';
import { Dialog } from '../../shared/components/dialog/dialog';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { ScoreBadge } from '../../shared/components/score-badge/score-badge';
import { Skeleton } from '../../shared/components/skeleton/skeleton';
import { AssessmentFormDialog } from './assessment-form-dialog';
import { AssessmentTypeChip } from './assessment-type-chip';
import { AssessmentScores } from './assessments.models';
import { AssessmentsService } from './assessments.service';
import { GradeSheet } from './grade-sheet';

/** /assessments/:id – an assessment's details, statistics and the grade sheet of the class. */
@Component({
  selector: 'app-assessment-details-page',
  imports: [
    AssessmentFormDialog,
    AssessmentTypeChip,
    Card,
    DatePipe,
    DecimalPipe,
    Dialog,
    EmptyState,
    GradeSheet,
    RouterLink,
    ScoreBadge,
    Skeleton,
  ],
  templateUrl: './assessment-details-page.html',
  styleUrl: './assessment-details-page.scss',
})
export class AssessmentDetailsPage {
  private readonly assessmentsService = inject(AssessmentsService);
  private readonly router = inject(Router);

  /** The :id route parameter (withComponentInputBinding). */
  readonly id = input.required({ transform: numberAttribute });

  /** The grade sheet response also contains the assessment itself, so one request is enough. */
  protected readonly sheet = rxResource({
    params: () => this.id(),
    stream: ({ params: id }) => this.assessmentsService.getScores(id),
  });

  protected readonly errorMessage = computed(() =>
    this.sheet.error() ? toErrorMessage(this.sheet.error()) : null,
  );

  protected readonly formOpen = signal(false);
  protected readonly deleteOpen = signal(false);
  protected readonly deleting = signal(false);
  protected readonly deleteError = signal<string | null>(null);

  /** Show the statistics the server calculated for the scores that were just saved. */
  protected onScoresSaved(sheet: AssessmentScores): void {
    this.sheet.set(sheet);
  }

  protected onEdited(): void {
    this.formOpen.set(false);
    this.sheet.reload();
  }

  protected askDelete(): void {
    this.deleteError.set(null);
    this.deleteOpen.set(true);
  }

  protected confirmDelete(): void {
    this.deleting.set(true);
    this.assessmentsService.deleteAssessment(this.id()).subscribe({
      next: () => void this.router.navigate(['/assessments']),
      error: (error: unknown) => {
        this.deleting.set(false);
        this.deleteError.set(toErrorMessage(error));
      },
    });
  }
}
