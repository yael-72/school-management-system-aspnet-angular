import { Component } from '@angular/core';
import { Card } from '../../shared/components/card/card';
import { EmptyState } from '../../shared/components/empty-state/empty-state';

/** Placeholder page; the real content is built in a later milestone. */
@Component({
  selector: 'app-classes-page',
  imports: [Card, EmptyState],
  template: `
    <app-card>
      <app-empty-state icon="school" title="כיתות – בקרוב" message="כאן תוצג רשת כרטיסי הכיתות עם סינון לפי שכבה." />
    </app-card>
  `,
})
export class ClassesPage {}
