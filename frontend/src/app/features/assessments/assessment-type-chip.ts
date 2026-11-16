import { Component, computed, input } from '@angular/core';
import { BadgeTone, StatusBadge } from '../../shared/components/status-badge/status-badge';
import { AssessmentType, assessmentTypeLabel } from './assessments.models';

const TYPE_TONES: Readonly<Record<AssessmentType, BadgeTone>> = {
  Exam: 'primary',
  Quiz: 'info',
  Assignment: 'success',
};

/** "מבחן" / "בוחן" / "עבודה" as a colored badge. */
@Component({
  selector: 'app-assessment-type-chip',
  imports: [StatusBadge],
  template: `<app-status-badge [tone]="tone()">{{ label() }}</app-status-badge>`,
})
export class AssessmentTypeChip {
  readonly type = input.required<AssessmentType>();
  protected readonly label = computed(() => assessmentTypeLabel(this.type()));
  protected readonly tone = computed(() => TYPE_TONES[this.type()]);
}
