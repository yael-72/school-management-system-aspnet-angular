import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Avatar } from '../../../shared/components/avatar/avatar';
import { EmptyState } from '../../../shared/components/empty-state/empty-state';
import { ScoreBadge } from '../../../shared/components/score-badge/score-badge';
import { TopStudent } from '../dashboard.models';

/** The students with the highest average (Horizon "Check Table"). Used on the dashboard and the classes page. */
@Component({
  selector: 'app-top-students',
  imports: [Avatar, EmptyState, RouterLink, ScoreBadge],
  template: `
    @if (students().length === 0) {
      <app-empty-state icon="emoji_events" title="אין עדיין ציונים" />
    } @else {
      <div class="table-scroll">
        <table class="data-table">
          <thead>
            <tr>
              <th>#</th>
              <th>תלמיד</th>
              <th>כיתה</th>
              <th>ממוצע</th>
            </tr>
          </thead>
          <tbody>
            @for (student of students(); track student.studentId; let rank = $index) {
              <tr>
                <td class="rank">{{ rank + 1 }}</td>
                <td>
                  <a class="student-link" [routerLink]="['/students', student.studentId]">
                    <app-avatar [name]="student.firstName + ' ' + student.lastName" [size]="30" />
                    {{ student.firstName }} {{ student.lastName }}
                  </a>
                </td>
                <td>{{ student.className }}</td>
                <td><app-score-badge [score]="student.averageScore" /></td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    }
  `,
  styles: `
    .rank {
      color: var(--color-text-secondary);
    }
    .student-link {
      display: inline-flex;
      align-items: center;
      gap: var(--space-3);
      &:hover {
        color: var(--color-primary);
      }
    }
  `,
})
export class TopStudents {
  readonly students = input.required<TopStudent[]>();
}
