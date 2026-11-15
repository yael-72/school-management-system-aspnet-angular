import { computed, inject } from '@angular/core';
import { ThemeService } from '../../core/theme/theme.service';

/** Colors a chart needs, read from the design tokens in styles/_tokens.scss. */
export interface ChartColors {
  primary: string;
  text: string;
  textSecondary: string;
  border: string;
  surface: string;
  success: string;
  info: string;
  warning: string;
  danger: string;
  fontFamily: string;
}

/**
 * ApexCharts draws SVG with plain color values, so it cannot use our CSS variables directly.
 * This reads the current values of the variables from the page.
 */
export function readChartColors(element: Element = document.documentElement): ChartColors {
  const style = getComputedStyle(element);
  const token = (name: string) => style.getPropertyValue(name).trim();
  return {
    primary: token('--color-primary'),
    text: token('--color-text'),
    textSecondary: token('--color-text-secondary'),
    border: token('--color-border'),
    surface: token('--color-surface'),
    success: token('--color-success'),
    info: token('--color-info'),
    warning: token('--color-warning'),
    danger: token('--color-danger'),
    fontFamily: token('--font-family'),
  };
}

/**
 * A signal with the chart colors of the current theme. It is re-read whenever the user switches
 * between light and dark mode, so charts that build their options from it are redrawn in the new
 * colors. Call it in a component's field initializer (it uses inject()).
 */
export function injectChartColors() {
  const themeService = inject(ThemeService);
  return computed(() => {
    themeService.theme(); // dependency: recalculate when the theme changes
    return readChartColors();
  });
}
