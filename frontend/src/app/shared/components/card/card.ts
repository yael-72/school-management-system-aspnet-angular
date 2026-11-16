import { Component, input } from '@angular/core';

/**
 * The white rounded container used for every block of content in the design.
 * Usage: <app-card title="כותרת"> ...content... </app-card>
 * An element marked `card-action` goes next to the title (for example a "הכול" link):
 * <app-card title="כותרת"><a card-action routerLink="...">הכול</a> ... </app-card>
 */
@Component({
  selector: 'app-card',
  template: `
    @if (title()) {
      <div class="card__header">
        <h2 class="card__title">{{ title() }}</h2>
        <ng-content select="[card-action]" />
      </div>
    }
    <ng-content />
  `,
  styles: `
    :host {
      display: block;
      padding: var(--space-5);
      background: var(--color-surface);
      border-radius: var(--radius-card);
      box-shadow: var(--shadow-card);
    }
    .card__header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--space-3);
      margin-block-end: var(--space-4);
    }
    .card__title {
      font-size: var(--font-size-lg);
      font-weight: 700;
    }
  `,
})
export class Card {
  readonly title = input<string>();
}
