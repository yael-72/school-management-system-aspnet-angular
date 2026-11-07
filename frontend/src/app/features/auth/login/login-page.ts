import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Card } from '../../../shared/components/card/card';
import { EmptyState } from '../../../shared/components/empty-state/empty-state';

/** Placeholder; the sign-in form is built in milestone 2 (Auth). */
@Component({
  selector: 'app-login-page',
  imports: [Card, EmptyState, RouterLink],
  template: `
    <app-card>
      <app-empty-state icon="lock" title="התחברות – בקרוב" message="מסך ההתחברות ייבנה בשלב הבא.">
        <a routerLink="/dashboard">מעבר ללוח הבקרה</a>
      </app-empty-state>
    </app-card>
  `,
  styles: `
    :host {
      display: grid;
      place-items: center;
      min-block-size: 100vh;
      padding: var(--space-4);
    }
    a {
      color: var(--color-primary);
      font-weight: 700;
    }
  `,
})
export class LoginPage {}
