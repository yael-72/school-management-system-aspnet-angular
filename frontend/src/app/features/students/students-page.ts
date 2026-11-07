import { Component } from '@angular/core';
import { Card } from '../../shared/components/card/card';
import { EmptyState } from '../../shared/components/empty-state/empty-state';

/** Placeholder page; the real content is built in a later milestone. */
@Component({
  selector: 'app-students-page',
  imports: [Card, EmptyState],
  template: `
    <app-card>
      <app-empty-state icon="groups" title="תלמידים – בקרוב" message="כאן תוצג טבלת התלמידים עם חיפוש, מיון ודפדוף." />
    </app-card>
  `,
})
export class StudentsPage {}
