import { Component, input } from '@angular/core';

/** A grey pulsing block shown in place of content while it loads. */
@Component({
  selector: 'app-skeleton',
  template: '',
  host: {
    'aria-hidden': 'true',
    '[style.inline-size]': 'width()',
    '[style.block-size]': 'height()',
    '[style.border-radius]': 'radius()',
  },
  styles: `
    :host {
      display: block;
      background: var(--color-surface-muted);
      animation: skeleton-pulse 1.4s ease-in-out infinite;
    }
    @keyframes skeleton-pulse {
      50% {
        opacity: 0.5;
      }
    }
    @media (prefers-reduced-motion: reduce) {
      :host {
        animation: none;
      }
    }
  `,
})
export class Skeleton {
  readonly width = input('100%');
  readonly height = input('16px');
  readonly radius = input('8px');
}
