import { Component } from '@angular/core';
import { Card } from '../../shared/components/card/card';
import { EmptyState } from '../../shared/components/empty-state/empty-state';

/** Placeholder page; the real content is built in a later milestone. */
@Component({
  selector: 'app-dashboard-page',
  imports: [Card, EmptyState],
  template: `
    <app-card>
      <app-empty-state icon="dashboard" title="לוח בקרה – בקרוב" message="כאן יוצגו נתוני בית הספר: כרטיסי סטטיסטיקה, גרפים, משימות ולוח שנה." />
    </app-card>
  `,
})
export class DashboardPage {}
