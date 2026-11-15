import { DOCUMENT } from '@angular/common';
import { Injectable, effect, inject, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

const STORAGE_KEY = 'theme';

/**
 * Holds the current color theme in a signal and applies it to <html data-theme="...">.
 * The CSS variables in styles/_tokens.scss switch automatically based on that attribute.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);

  readonly theme = signal<Theme>(this.loadInitialTheme());

  constructor() {
    // Runs whenever the theme signal changes: update the page and remember the choice.
    effect(() => {
      const theme = this.theme();
      this.document.documentElement.dataset['theme'] = theme;
      localStorage.setItem(STORAGE_KEY, theme);
    });
  }

  toggle(): void {
    this.theme.update((current) => (current === 'light' ? 'dark' : 'light'));
  }

  private loadInitialTheme(): Theme {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved === 'light' || saved === 'dark') {
      return saved;
    }
    const prefersDark = this.document.defaultView?.matchMedia?.('(prefers-color-scheme: dark)').matches;
    return prefersDark ? 'dark' : 'light';
  }
}
