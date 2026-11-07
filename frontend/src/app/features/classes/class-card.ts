import { Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ScoreBadge } from '../../shared/components/score-badge/score-badge';
import { ClassSummary, GRADE_LEVEL_NAMES } from './classes.models';

/**
 * One class in the grid. Follows the Horizon "NFT card": a colored banner on top (instead of the
 * NFT image), the name and owner line, and a footer row with a value and a dark pill button.
 */
@Component({
  selector: 'app-class-card',
  imports: [RouterLink, ScoreBadge],
  templateUrl: './class-card.html',
  styleUrl: './class-card.scss',
})
export class ClassCard {
  readonly schoolClass = input.required<ClassSummary>();
  /** Shows the edit and delete buttons (admins only). */
  readonly canManage = input(false);
  readonly edit = output<ClassSummary>();
  readonly remove = output<ClassSummary>();

  protected readonly gradeNames = GRADE_LEVEL_NAMES;
}
