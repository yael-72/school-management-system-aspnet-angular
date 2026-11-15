import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, input, linkedSignal, output, signal } from '@angular/core';
import { toErrorMessage } from '../../core/api/api-error';
import { Avatar } from '../../shared/components/avatar/avatar';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { ScoreBadge } from '../../shared/components/score-badge/score-badge';
import { todayIsoDate } from '../../shared/utils/dates';
import { AssessmentScores, StudentScore } from './assessments.models';
import { AssessmentsService } from './assessments.service';

/** A row while it is being edited: the score is kept as the text the user typed. */
interface GradeRow {
  student: StudentScore;
  scoreText: string;
  comment: string;
}

/** Up to 2 decimal places, like the API ("92", "17.5", "18.25"). */
const SCORE_PATTERN = /^\d+(\.\d{1,2})?$/;

function toRow(student: StudentScore): GradeRow {
  return { student, scoreText: student.score?.toString() ?? '', comment: student.comment ?? '' };
}

/**
 * The grades table of one assessment: a row per student with a score input, the score as a
 * color-coded percentage, and a comment. Changes are kept locally and sent with one "save"
 * (PUT /api/assessments/{id}/scores). Read-only for users who may not grade this subject.
 */
@Component({
  selector: 'app-grade-sheet',
  imports: [Avatar, DecimalPipe, EmptyState, ScoreBadge],
  templateUrl: './grade-sheet.html',
  styleUrl: './grade-sheet.scss',
})
export class GradeSheet {
  private readonly assessmentsService = inject(AssessmentsService);

  readonly sheet = input.required<AssessmentScores>();
  /** Emits the grade sheet as the server saved it. */
  readonly saved = output<AssessmentScores>();

  protected readonly assessment = computed(() => this.sheet().assessment);
  protected readonly maxScore = computed(() => this.assessment().maxScore);

  /** Scores are entered from the day of the assessment (the API has the same rule). */
  protected readonly isUpcoming = computed(() => this.assessment().date > todayIsoDate());
  protected readonly editable = computed(() => this.assessment().canEdit && !this.isUpcoming());

  /** Reset to the loaded sheet whenever a new sheet arrives (first load or after saving). */
  protected readonly rows = linkedSignal(() => this.sheet().students.map(toRow));

  protected readonly changedRows = computed(() =>
    this.rows().filter((row) => {
      const original = toRow(row.student);
      return row.scoreText.trim() !== original.scoreText || row.comment.trim() !== original.comment;
    }),
  );

  protected readonly invalidCount = computed(
    () => this.rows().filter((row) => !this.isValid(row)).length,
  );

  protected readonly saving = signal(false);
  protected readonly saveError = signal<string | null>(null);
  protected readonly savedMessage = signal(false);

  /** The score as a number, or null when the input is empty or not a valid score. */
  protected parseScore(row: GradeRow): number | null {
    const text = row.scoreText.trim();
    return text !== '' && SCORE_PATTERN.test(text) ? Number(text) : null;
  }

  protected isValid(row: GradeRow): boolean {
    const text = row.scoreText.trim();
    if (text === '') {
      return true;
    }
    const score = this.parseScore(row);
    return score !== null && score <= this.maxScore();
  }

  /** Live percentage while typing, so the color shows how good the score is. */
  protected percent(row: GradeRow): number | null {
    const score = this.isValid(row) ? this.parseScore(row) : null;
    return score === null ? null : Math.round((score * 1000) / this.maxScore()) / 10;
  }

  protected setScore(studentId: number, scoreText: string): void {
    this.updateRow(studentId, { scoreText });
  }

  protected setComment(studentId: number, comment: string): void {
    this.updateRow(studentId, { comment });
  }

  protected save(): void {
    // Only the rows that changed; an emptied row clears that student's score on the server.
    const scores = this.changedRows().map((row) => ({
      studentId: row.student.studentId,
      score: this.parseScore(row),
      comment: row.comment.trim() || null,
    }));

    this.saving.set(true);
    this.clearMessages();
    this.assessmentsService.saveScores(this.assessment().id, { scores }).subscribe({
      next: (sheet) => {
        this.saving.set(false);
        this.savedMessage.set(true);
        this.saved.emit(sheet);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.saveError.set(toErrorMessage(error));
      },
    });
  }

  private updateRow(studentId: number, changes: Partial<GradeRow>): void {
    this.rows.update((rows) =>
      rows.map((row) => (row.student.studentId === studentId ? { ...row, ...changes } : row)),
    );
    this.clearMessages();
  }

  private clearMessages(): void {
    this.savedMessage.set(false);
    this.saveError.set(null);
  }
}
