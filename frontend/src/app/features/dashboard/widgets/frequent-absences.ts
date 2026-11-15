import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Avatar } from '../../../shared/components/avatar/avatar';
import { EmptyState } from '../../../shared/components/empty-state/empty-state';
import { FrequentAbsence } from '../dashboard.models';

/** Students who were absent often in the last 30 days – a list the homeroom teacher should act on. */
@Component({
  selector: 'app-frequent-absences',
  imports: [Avatar, EmptyState, RouterLink],
  template: `
    @if (students().length === 0) {
      <app-empty-state
        icon="sentiment_satisfied"
        title="אין היעדרויות חריגות"
        message="אף תלמיד לא נעדר 4 פעמים או יותר ב-30 הימים האחרונים."
      />
    } @else {
      <ul class="absences">
        @for (student of students(); track student.studentId) {
          <li>
            <a class="absences__item" [routerLink]="['/students', student.studentId]">
              <app-avatar [name]="student.firstName + ' ' + student.lastName" [size]="38" />
              <span class="absences__name">
                {{ student.firstName }} {{ student.lastName }}
                <span class="absences__class">כיתה {{ student.className }}</span>
              </span>
              <span class="absences__count">{{ student.absenceCount }} היעדרויות</span>
            </a>
          </li>
        }
      </ul>
    }
  `,
  styles: `
    .absences {
      display: flex;
      flex-direction: column;
      gap: var(--space-2);
      padding: 0;
      margin: 0;
      list-style: none;
    }
    .absences__item {
      display: flex;
      align-items: center;
      gap: var(--space-3);
      padding: var(--space-2);
      border-radius: var(--radius-control);
      transition: background-color var(--transition-fast);
      &:hover {
        background: var(--color-surface-muted);
      }
    }
    .absences__name {
      display: flex;
      flex: 1;
      flex-direction: column;
      font-size: var(--font-size-sm);
      font-weight: 700;
    }
    .absences__class {
      font-size: var(--font-size-xs);
      font-weight: 500;
      color: var(--color-text-secondary);
    }
    .absences__count {
      padding: 4px 10px;
      font-size: var(--font-size-xs);
      font-weight: 700;
      white-space: nowrap;
      color: var(--color-danger);
      background: color-mix(in srgb, var(--color-danger) 12%, transparent);
      border-radius: var(--radius-pill);
    }
  `,
})
export class FrequentAbsences {
  readonly students = input.required<FrequentAbsence[]>();
}
