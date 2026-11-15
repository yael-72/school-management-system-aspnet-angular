import { Component, computed, input } from '@angular/core';
import { ApexOptions, ChartComponent } from 'ng-apexcharts';
import { injectChartColors } from '../../../shared/charts/chart-colors';
import { DailyAttendance } from '../dashboard.models';

/** "א׳ 9.11" – the Hebrew weekday letter and the day of the month. */
const dayFormat = new Intl.DateTimeFormat('he-IL', { weekday: 'narrow' });

function dayLabel(isoDate: string): string {
  const [year, month, day] = isoDate.split('-').map(Number);
  const date = new Date(year, month - 1, day);
  return `${dayFormat.format(date)} ${day}.${month}`;
}

/**
 * Stacked bars per school day (Horizon "Weekly Revenue" chart). Each status has the same color
 * as on the attendance page: present green, late orange, excused blue, absent red.
 */
@Component({
  selector: 'app-weekly-attendance-chart',
  imports: [ChartComponent],
  template: `
    <div dir="ltr">
      <apx-chart
        [chart]="options().chart"
        [series]="options().series"
        [colors]="options().colors"
        [plotOptions]="options().plotOptions"
        [dataLabels]="options().dataLabels"
        [xaxis]="options().xaxis"
        [yaxis]="options().yaxis"
        [grid]="options().grid"
        [legend]="options().legend"
        [tooltip]="options().tooltip"
      />
    </div>
  `,
})
export class WeeklyAttendanceChart {
  readonly days = input.required<DailyAttendance[]>();

  private readonly colors = injectChartColors();

  protected readonly options = computed<ApexOptions>(() => {
    const colors = this.colors();
    const days = this.days();
    return {
      chart: {
        type: 'bar',
        stacked: true,
        height: 250,
        fontFamily: colors.fontFamily,
        toolbar: { show: false },
        animations: { enabled: true, dynamicAnimation: { enabled: false } },
      },
      series: [
        { name: 'נוכחים', data: days.map((d) => d.present) },
        { name: 'איחור', data: days.map((d) => d.late) },
        { name: 'חיסור מוצדק', data: days.map((d) => d.excused) },
        { name: 'חסרים', data: days.map((d) => d.absent) },
      ],
      colors: [colors.success, colors.warning, colors.info, colors.danger],
      plotOptions: {
        bar: {
          columnWidth: '22%',
          borderRadius: 8,
          borderRadiusApplication: 'end',
          borderRadiusWhenStacked: 'last',
        },
      },
      dataLabels: { enabled: false },
      xaxis: {
        categories: days.map((d) => dayLabel(d.date)),
        axisBorder: { show: false },
        axisTicks: { show: false },
        labels: { style: { colors: colors.textSecondary, fontSize: '12px' } },
      },
      yaxis: { show: false },
      grid: { show: false },
      legend: {
        position: 'bottom',
        fontSize: '12px',
        labels: { colors: colors.textSecondary },
        markers: { size: 5, offsetX: -2 },
        itemMargin: { horizontal: 8 },
      },
      tooltip: { theme: 'dark', shared: true, intersect: false },
    };
  });
}
