import { Component } from '@angular/core';
import { Card } from '../../shared/components/card/card';
import { EmptyState } from '../../shared/components/empty-state/empty-state';

/** Placeholder page; the real content is built in a later milestone. */
@Component({
  selector: 'app-assessments-page',
  imports: [Card, EmptyState],
  template: `
    <app-card>
      <app-empty-state icon="assignment" title="מבחנים וציונים – בקרוב" message="כאן יוצגו המבחנים, הזנת ציונים וטבלת ציונים צבעונית." />
    </app-card>
  `,
})
export class AssessmentsPage {}
