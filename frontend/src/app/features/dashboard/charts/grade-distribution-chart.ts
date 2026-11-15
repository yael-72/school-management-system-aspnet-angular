import { DecimalPipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { ApexOptions, ChartComponent } from 'ng-apexcharts';
import { injectChartColors } from '../../../shared/charts/chart-colors';
import { GradeDistribution } from '../dashboard.models';

interface Slice {
  key: keyof GradeDistribution;
  label: string;
  range: string;
}

/** The same bands and colors as the score badges (see ScoreBadge / GradeBands in the API). */
const SLICES: readonly Slice[] = [
  { key: 'excellent', label: 'מצוין', range: '90–100' },
  { key: 'good', label: 'טוב', range: '75–89' },
  { key: 'fair', label: 'מספיק', range: '55–74' },
  { key: 'low', label: 'נמוך', range: '0–54' },
];

/** Pie of how many scores fall in each grade band (Horizon "Your Pie Chart") with a legend card. */
@Component({
  selector: 'app-grade-distribution-chart',
  imports: [ChartComponent, DecimalPipe],
  template: `
    <div dir="ltr" class="pie">
      <apx-chart
        [chart]="options().chart"
        [series]="options().series"
        [labels]="options().labels"
        [colors]="options().colors"
        [stroke]="options().stroke"
        [dataLabels]="options().dataLabels"
        [legend]="options().legend"
        [tooltip]="options().tooltip"
        [states]="options().states"
      />
    </div>
    <ul class="legend">
      @for (slice of legend(); track slice.key) {
        <li>
          <span class="legend__label">
            <span class="legend__dot" [style.background]="slice.color"></span>
            {{ slice.label }} ({{ slice.range }})
          </span>
          <span class="legend__value">{{ slice.percent | number: '1.0-0' }}%</span>
        </li>
      }
    </ul>
  `,
  styles: `
    .legend {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: var(--space-3) var(--space-4);
      padding: var(--space-4);
      margin: var(--space-4) 0 0;
      list-style: none;
      border-radius: var(--radius-card);
      box-shadow: var(--shadow-soft);
      background: var(--color-surface);
    }
    li {
      display: flex;
      flex-direction: column;
      gap: 2px;
    }
    .legend__label {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      font-size: var(--font-size-xs);
      color: var(--color-text-secondary);
    }
    .legend__dot {
      inline-size: 8px;
      block-size: 8px;
      border-radius: 50%;
    }
    .legend__value {
      font-size: var(--font-size-md);
      font-weight: 700;
    }
  `,
})
export class GradeDistributionChart {
  readonly distribution = input.required<GradeDistribution>();

  private readonly colors = injectChartColors();

  private readonly sliceColors = computed(() => {
    const colors = this.colors();
    return [colors.success, colors.info, colors.warning, colors.danger];
  });

  protected readonly legend = computed(() => {
    const distribution = this.distribution();
    const total = SLICES.reduce((sum, slice) => sum + distribution[slice.key], 0);
    return SLICES.map((slice, i) => ({
      ...slice,
      color: this.sliceColors()[i],
      percent: total === 0 ? 0 : (distribution[slice.key] * 100) / total,
    }));
  });

  protected readonly options = computed<ApexOptions>(() => {
    const colors = this.colors();
    const distribution = this.distribution();
    return {
      chart: {
        type: 'pie',
        height: 200,
        fontFamily: colors.fontFamily,
        animations: { enabled: true, dynamicAnimation: { enabled: false } },
      },
      series: SLICES.map((slice) => distribution[slice.key]),
      labels: SLICES.map((slice) => slice.label),
      colors: this.sliceColors(),
      stroke: { width: 2, colors: [colors.surface] },
      dataLabels: { enabled: false },
      legend: { show: false },
      tooltip: { theme: 'dark', fillSeriesColor: false },
      states: { hover: { filter: { type: 'none' } } },
    };
  });
}
