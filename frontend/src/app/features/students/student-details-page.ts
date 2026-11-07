import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, inject, input, numberAttribute } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { toErrorMessage } from '../../core/api/api-error';
import { Avatar } from '../../shared/components/avatar/avatar';
import { Card } from '../../shared/components/card/card';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { ScoreBadge } from '../../shared/components/score-badge/score-badge';
import { Skeleton } from '../../shared/components/skeleton/skeleton';
import { StudentsService } from './students.service';

/** /students/:id – the student's details, averages, attendance and latest scores. */
@Component({
  selector: 'app-student-details-page',
  imports: [Avatar, Card, DatePipe, DecimalPipe, EmptyState, RouterLink, ScoreBadge, Skeleton],
  templateUrl: './student-details-page.html',
  styleUrl: './student-details-page.scss',
})
export class StudentDetailsPage {
  private readonly studentsService = inject(StudentsService);

  /** The :id route parameter (withComponentInputBinding). */
  readonly id = input.required({ transform: numberAttribute });

  protected readonly summary = rxResource({
    params: () => this.id(),
    stream: ({ params: id }) => this.studentsService.getSummary(id),
  });

  protected readonly errorMessage = computed(() =>
    this.summary.error() ? toErrorMessage(this.summary.error()) : null,
  );
}
