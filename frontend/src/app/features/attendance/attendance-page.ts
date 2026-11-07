import { Component } from '@angular/core';
import { Card } from '../../shared/components/card/card';
import { EmptyState } from '../../shared/components/empty-state/empty-state';

/** Placeholder page; the real content is built in a later milestone. */
@Component({
  selector: 'app-attendance-page',
  imports: [Card, EmptyState],
  template: `
    <app-card>
      <app-empty-state icon="fact_check" title="נוכחות – בקרוב" message="כאן ניתן יהיה לסמן נוכחות לכיתה שלמה ביום מסוים." />
    </app-card>
  `,
})
export class AttendancePage {}
