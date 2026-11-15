import { Component, input } from '@angular/core';

/**
 * The white rounded container used for every block of content in the design.
 * Usage: <app-card title="כותרת"> ...content... </app-card>
 */
@Component({
  selector: 'app-card',
  template: `
    @if (title()) {
      <h2 class="card__title">{{ title() }}</h2>
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
    .card__title {
      font-size: var(--font-size-lg);
      font-weight: 700;
      margin-block-end: var(--space-4);
    }
  `,
})
export class Card {
  readonly title = input<string>();
}
