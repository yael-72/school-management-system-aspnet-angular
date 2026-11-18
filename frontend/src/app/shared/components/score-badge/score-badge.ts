import { DecimalPipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { BadgeTone, StatusBadge } from '../status-badge/status-badge';

export type ScoreLevel = 'excellent' | 'good' | 'fair' | 'low';

/** Grade bands used for color-coding scores everywhere in the app (same numbers as GradeBands in the API). */
export function scoreLevel(score: number): ScoreLevel {
  if (score >= 90) return 'excellent';
  if (score >= 75) return 'good';
  if (score >= 55) return 'fair';
  return 'low';
}

const LEVEL_TONES: Readonly<Record<ScoreLevel, BadgeTone>> = {
  excellent: 'success',
  good: 'info',
  fair: 'warning',
  low: 'danger',
};

/** A score (0–100) in a pill colored by its grade band; "—" when there is no score yet. */
@Component({
  selector: 'app-score-badge',
  imports: [DecimalPipe, StatusBadge],
  template: `
    @if (score() === null) {
      <span class="score-empty">—</span>
    } @else {
      <app-status-badge size="md" [tone]="tone()" [attr.data-level]="level()">
        {{ score() | number: '1.0-1' }}
      </app-status-badge>
    }
  `,
  styles: `
    .score-empty {
      color: var(--color-text-secondary);
    }
  `,
})
export class ScoreBadge {
  readonly score = input.required<number | null>();
  protected readonly level = computed(() => scoreLevel(this.score() ?? 0));
  protected readonly tone = computed(() => LEVEL_TONES[this.level()]);
}
