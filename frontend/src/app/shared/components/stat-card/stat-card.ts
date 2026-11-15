import { Component, input } from '@angular/core';

/**
 * A small KPI card (Horizon "mini statistics"): an icon in a circle, a label and a big value.
 * Usage: <app-stat-card icon="groups" label="תלמידים" [value]="210" />
 * The value can also be projected, for example a <app-score-badge>.
 */
@Component({
  selector: 'app-stat-card',
  template: `
    @if (icon()) {
      <span class="stat-card__icon" [class.stat-card__icon--accent]="accent()" aria-hidden="true">
        <span class="icon">{{ icon() }}</span>
      </span>
    }
    <div class="stat-card__text">
      <span class="stat-card__label">{{ label() }}</span>
      <span class="stat-card__value">
        {{ value() }}
        <ng-content />
      </span>
    </div>
  `,
  styles: `
    :host {
      display: flex;
      align-items: center;
      gap: 18px;
      min-block-size: 97px;
      padding: var(--space-4) var(--space-5);
      background: var(--color-surface);
      border-radius: var(--radius-card);
      box-shadow: var(--shadow-card);
    }
    .stat-card__icon {
      display: inline-flex;
      flex: none;
      align-items: center;
      justify-content: center;
      inline-size: 56px;
      block-size: 56px;
      color: var(--color-primary);
      background: var(--color-surface-muted);
      border-radius: 50%;
    }
    .stat-card__icon--accent {
      color: var(--color-text-on-primary);
      background: var(--gradient-brand);
    }
    .stat-card__text {
      display: flex;
      flex-direction: column;
      gap: 2px;
      min-inline-size: 0;
    }
    .stat-card__label {
      font-size: var(--font-size-sm);
      font-weight: 500;
      color: var(--color-text-secondary);
    }
    .stat-card__value {
      font-size: var(--font-size-lg);
      font-weight: 700;
      line-height: 1.3;
      color: var(--color-text);
    }
  `,
})
export class StatCard {
  readonly label = input.required<string>();
  readonly value = input<string | number | null>(null);
  /** Material Symbols icon name. */
  readonly icon = input<string>();
  /** Use the brand gradient for the icon circle, to highlight one card. */
  readonly accent = input(false);
}
