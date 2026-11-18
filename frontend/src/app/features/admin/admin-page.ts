import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Card } from '../../shared/components/card/card';

/** The admin area: one card with tabs, each tab a child route (so it can be linked to). */
@Component({
  selector: 'app-admin-page',
  imports: [Card, RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <app-card>
      <div class="admin-header">
        <h2 class="admin-header__title">ניהול בית הספר</h2>
        <nav class="pill-tabs" aria-label="אזורי ניהול">
          @for (tab of tabs; track tab.path) {
            <a
              class="pill-tabs__tab"
              [routerLink]="tab.path"
              routerLinkActive="is-active"
              ariaCurrentWhenActive="page"
            >
              {{ tab.label }}
            </a>
          }
        </nav>
      </div>
      <router-outlet />
    </app-card>
  `,
  styles: `
    .admin-header {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: var(--space-4);
      margin-block-end: var(--space-5);
    }
    .admin-header__title {
      font-size: var(--font-size-lg);
      font-weight: 700;
    }
  `,
})
export class AdminPage {
  protected readonly tabs = [
    { path: 'users', label: 'משתמשים' },
    { path: 'subjects', label: 'מקצועות' },
    { path: 'assignments', label: 'שיבוץ מורים' },
  ];
}
