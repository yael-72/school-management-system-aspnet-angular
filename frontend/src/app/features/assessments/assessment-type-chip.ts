import { Component, computed, input } from '@angular/core';
import { AssessmentType, assessmentTypeLabel } from './assessments.models';

/** "מבחן" / "בוחן" / "עבודה" in a small colored pill. */
@Component({
  selector: 'app-assessment-type-chip',
  template: `<span [class]="'type-chip type-chip--' + type()">{{ label() }}</span>`,
  styles: `
    .type-chip {
      --chip-color: var(--color-primary);
      display: inline-block;
      padding: 2px 10px;
      font-size: var(--font-size-xs);
      font-weight: 700;
      color: var(--chip-color);
      background: color-mix(in srgb, var(--chip-color) 12%, transparent);
      border-radius: var(--radius-pill);
    }
    .type-chip--Quiz {
      --chip-color: var(--color-info);
    }
    .type-chip--Assignment {
      --chip-color: var(--color-success);
    }
  `,
})
export class AssessmentTypeChip {
  readonly type = input.required<AssessmentType>();
  protected readonly label = computed(() => assessmentTypeLabel(this.type()));
}
