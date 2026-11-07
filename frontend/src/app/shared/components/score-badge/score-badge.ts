import { Component, computed, input } from '@angular/core';
import { DecimalPipe } from '@angular/common';

export type ScoreLevel = 'excellent' | 'good' | 'fair' | 'low';

/** Grade bands used for color-coding scores everywhere in the app. */
export function scoreLevel(score: number): ScoreLevel {
  if (score >= 90) return 'excellent';
  if (score >= 75) return 'good';
  if (score >= 55) return 'fair';
  return 'low';
}

/** A score (0–100) in a small pill colored by its grade band; "—" when there is no score yet. */
@Component({
  selector: 'app-score-badge',
  imports: [DecimalPipe],
  template: `
    @if (score() === null) {
      <span class="score score--empty">—</span>
    } @else {
      <span [class]="'score score--' + level()">{{ score() | number: '1.0-1' }}</span>
    }
  `,
  styles: `
    .score {
      display: inline-block;
      min-inline-size: 48px;
      padding: 4px 10px;
      font-size: var(--font-size-sm);
      font-weight: 700;
      text-align: center;
      border-radius: var(--radius-pill);
    }
    .score--excellent {
      color: var(--color-success);
      background: color-mix(in srgb, var(--color-success) 12%, transparent);
    }
    .score--good {
      color: var(--color-info);
      background: color-mix(in srgb, var(--color-info) 12%, transparent);
    }
    .score--fair {
      color: var(--color-warning);
      background: color-mix(in srgb, var(--color-warning) 14%, transparent);
    }
    .score--low {
      color: var(--color-danger);
      background: color-mix(in srgb, var(--color-danger) 12%, transparent);
    }
    .score--empty {
      color: var(--color-text-secondary);
    }
  `,
})
export class ScoreBadge {
  readonly score = input.required<number | null>();
  protected readonly level = computed(() => scoreLevel(this.score() ?? 0));
}
