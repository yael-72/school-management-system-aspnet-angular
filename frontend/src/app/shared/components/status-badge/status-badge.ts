import { Component, input } from '@angular/core';

/**
 * The color of a badge. Each tone is one design token, so meanings stay consistent across the app:
 * success = good / present / active, danger = problem / absent, warning = late, info = neutral info.
 */
export type BadgeTone = 'primary' | 'success' | 'info' | 'warning' | 'danger' | 'neutral';

/**
 * A small pill with a soft background in the tone's color (Horizon status chips), used for statuses,
 * counts and labels across the app. The text goes inside:
 * <app-status-badge tone="success">פעיל</app-status-badge>
 */
@Component({
  selector: 'app-status-badge',
  template: `<ng-content />`,
  host: { '[attr.data-tone]': 'tone()', '[attr.data-size]': 'size()' },
  styles: `
    :host {
      --badge-color: var(--color-primary);
      display: inline-block;
      padding: 3px 10px;
      font-size: var(--font-size-xs);
      font-weight: 700;
      line-height: 1.5;
      white-space: nowrap;
      color: var(--badge-color);
      background: color-mix(in srgb, var(--badge-color) 12%, transparent);
      border-radius: var(--radius-pill);
    }
    :host([data-size='md']) {
      min-inline-size: 48px;
      padding: 4px 10px;
      font-size: var(--font-size-sm);
      text-align: center;
    }
    :host([data-tone='success']) {
      --badge-color: var(--color-success);
    }
    :host([data-tone='info']) {
      --badge-color: var(--color-info);
    }
    :host([data-tone='warning']) {
      --badge-color: var(--color-warning);
    }
    :host([data-tone='danger']) {
      --badge-color: var(--color-danger);
    }
    :host([data-tone='neutral']) {
      color: var(--color-text-secondary);
      background: var(--color-surface-muted);
    }
  `,
})
export class StatusBadge {
  readonly tone = input<BadgeTone>('primary');
  /** "md" for numbers that should stand out (scores); "sm" for labels. */
  readonly size = input<'sm' | 'md'>('sm');
}
