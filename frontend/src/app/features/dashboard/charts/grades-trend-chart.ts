import { Component, computed, input } from '@angular/core';
import { ApexOptions, ChartComponent } from 'ng-apexcharts';
import { injectChartColors } from '../../../shared/charts/chart-colors';
import { MonthlyAverage } from '../dashboard.models';

/** Short Hebrew month names for the x-axis: "ספט׳", "אוק׳"... */
const monthFormat = new Intl.DateTimeFormat('he-IL', { month: 'short' });

/** 10 points below the lowest average, rounded down to a multiple of 10 (0 when there is no data). */
function axisMin(months: MonthlyAverage[]): number {
  const values = months.map((m) => m.averageScore).filter((v): v is number => v !== null);
  return values.length === 0 ? 0 : Math.max(0, Math.floor((Math.min(...values) - 10) / 10) * 10);
}

/** Smooth area line of the monthly average score (Horizon "This month" line chart). */
@Component({
  selector: 'app-grades-trend-chart',
  imports: [ChartComponent],
  template: `
    <!-- Charts are drawn left-to-right (time axis), also in the RTL layout. -->
    <div dir="ltr">
      <apx-chart
        [chart]="options().chart"
        [series]="options().series"
        [colors]="options().colors"
        [stroke]="options().stroke"
        [fill]="options().fill"
        [markers]="options().markers"
        [dataLabels]="options().dataLabels"
        [xaxis]="options().xaxis"
        [yaxis]="options().yaxis"
        [grid]="options().grid"
        [tooltip]="options().tooltip"
      />
    </div>
  `,
})
export class GradesTrendChart {
  readonly months = input.required<MonthlyAverage[]>();

  private readonly colors = injectChartColors();

  protected readonly options = computed<ApexOptions>(() => {
    const colors = this.colors();
    const months = this.months();
    return {
      chart: {
        type: 'area',
        height: 230,
        fontFamily: colors.fontFamily,
        toolbar: { show: false },
        zoom: { enabled: false },
        // Animating on every theme switch looks jumpy; animate only the first draw.
        animations: { enabled: true, dynamicAnimation: { enabled: false } },
      },
      series: [{ name: 'ממוצע', data: months.map((m) => m.averageScore) }],
      colors: [colors.primary],
      stroke: { curve: 'smooth', width: 4 },
      fill: {
        type: 'gradient',
        gradient: { shadeIntensity: 1, opacityFrom: 0.35, opacityTo: 0, stops: [0, 90] },
      },
      markers: { size: 0, hover: { size: 6 } },
      dataLabels: { enabled: false },
      xaxis: {
        categories: months.map((m) => monthFormat.format(new Date(m.year, m.month - 1, 1))),
        axisBorder: { show: false },
        axisTicks: { show: false },
        labels: { style: { colors: colors.textSecondary, fontSize: '12px' } },
        tooltip: { enabled: false },
      },
      // Start the axis a little below the lowest average, so a change of a few points is visible.
      yaxis: { show: false, min: axisMin(months), max: 100 },
      grid: { show: false, padding: { left: 8, right: 8 } },
      tooltip: { theme: 'dark', y: { formatter: (value: number) => `${value}` } },
    };
  });
}
