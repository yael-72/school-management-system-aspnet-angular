import { DatePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EmptyState } from '../../../shared/components/empty-state/empty-state';
import { ActivityKind, RecentActivity } from '../dashboard.models';

const KIND_INFO: Record<ActivityKind, { icon: string; label: string }> = {
  AssessmentCreated: { icon: 'add_circle', label: 'נוצר מבחן' },
  ScoresEntered: { icon: 'check_circle', label: 'הוזנו ציונים' },
};

/** The latest assessments created and graded (Horizon "Complex Table"). Used on the dashboard and the classes page. */
@Component({
  selector: 'app-recent-activity',
  imports: [DatePipe, EmptyState, RouterLink],
  template: `
    @if (items().length === 0) {
      <app-empty-state icon="history" title="אין פעילות אחרונה" />
    } @else {
      <ul class="activity">
        @for (item of items(); track item.kind + item.assessmentId) {
          <li class="activity__item">
            <span [class]="'activity__icon activity__icon--' + item.kind" aria-hidden="true">
              <span class="icon">{{ kindInfo[item.kind].icon }}</span>
            </span>
            <span class="activity__text">
              <span class="activity__kind">{{ kindInfo[item.kind].label }}</span>
              <a class="activity__title" [routerLink]="['/assessments', item.assessmentId]">{{
                item.title
              }}</a>
              <span class="activity__meta">
                כיתה {{ item.className }} · {{ item.subjectName }}
                @if (item.userName) {
                  · {{ item.userName }}
                }
              </span>
            </span>
            <time class="activity__date" [attr.datetime]="item.occurredAt">{{
              item.occurredAt | date: 'dd.MM.yyyy'
            }}</time>
          </li>
        }
      </ul>
    }
  `,
  styles: `
    .activity {
      display: flex;
      flex-direction: column;
      padding: 0;
      margin: 0;
      list-style: none;
    }
    .activity__item {
      display: flex;
      align-items: center;
      gap: var(--space-3);
      padding-block: var(--space-3);
      border-block-end: 1px solid var(--color-border);
      &:last-child {
        border-block-end: 0;
      }
    }
    .activity__icon {
      display: inline-flex;
      color: var(--color-success);
      .icon {
        font-size: 22px;
      }
    }
    .activity__icon--AssessmentCreated {
      color: var(--color-primary);
    }
    .activity__text {
      display: flex;
      flex: 1;
      flex-direction: column;
      min-inline-size: 0;
    }
    .activity__kind,
    .activity__meta {
      font-size: var(--font-size-xs);
      color: var(--color-text-secondary);
    }
    .activity__title {
      font-size: var(--font-size-sm);
      font-weight: 700;
      &:hover {
        color: var(--color-primary);
      }
    }
    .activity__date {
      font-size: var(--font-size-xs);
      font-weight: 500;
      white-space: nowrap;
      color: var(--color-text-secondary);
    }
  `,
})
export class RecentActivityList {
  readonly items = input.required<RecentActivity[]>();
  protected readonly kindInfo = KIND_INFO;
}
