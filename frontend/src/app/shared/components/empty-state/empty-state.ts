import { Component, input } from '@angular/core';

/** A friendly message shown when there is nothing to display. */
@Component({
  selector: 'app-empty-state',
  template: `
    <span class="icon empty-state__icon" aria-hidden="true">{{ icon() }}</span>
    <h3 class="empty-state__title">{{ title() }}</h3>
    @if (message()) {
      <p class="empty-state__message">{{ message() }}</p>
    }
    <ng-content />
  `,
  styles: `
    :host {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: var(--space-2);
      padding: var(--space-8) var(--space-4);
      text-align: center;
    }
    .empty-state__icon {
      font-size: 48px;
      color: var(--color-primary);
      background: var(--color-primary-soft);
      border-radius: 50%;
      padding: var(--space-4);
    }
    .empty-state__title {
      font-size: var(--font-size-lg);
    }
    .empty-state__message {
      color: var(--color-text-secondary);
      max-inline-size: 420px;
    }
  `,
})
export class EmptyState {
  readonly icon = input('inbox');
  readonly title = input.required<string>();
  readonly message = input<string>();
}
