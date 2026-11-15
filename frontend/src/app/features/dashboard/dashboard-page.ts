import { DecimalPipe } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { toErrorMessage } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { Card } from '../../shared/components/card/card';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { ScoreBadge } from '../../shared/components/score-badge/score-badge';
import { Skeleton } from '../../shared/components/skeleton/skeleton';
import { StatCard } from '../../shared/components/stat-card/stat-card';
import { MiniCalendar } from './calendar/mini-calendar';
import { GradeDistributionChart } from './charts/grade-distribution-chart';
import { GradesTrendChart } from './charts/grades-trend-chart';
import { WeeklyAttendanceChart } from './charts/weekly-attendance-chart';
import { DashboardService } from './dashboard.service';
import { TodoList } from './tasks/todo-list';
import { FrequentAbsences } from './widgets/frequent-absences';
import { RecentActivityList } from './widgets/recent-activity';
import { TopStudents } from './widgets/top-students';

const TREND_MONTHS = 6;

/**
 * /dashboard – the school at a glance (Horizon "Main Dashboard"). Each card loads its own data,
 * so one slow or failing request does not block the rest of the page.
 * An admin sees the whole school; a teacher sees her classes (the API scopes every number).
 */
@Component({
  selector: 'app-dashboard-page',
  imports: [
    Card,
    DecimalPipe,
    EmptyState,
    FrequentAbsences,
    GradeDistributionChart,
    GradesTrendChart,
    MiniCalendar,
    RecentActivityList,
    ScoreBadge,
    Skeleton,
    StatCard,
    TodoList,
    TopStudents,
    WeeklyAttendanceChart,
  ],
  templateUrl: './dashboard-page.html',
  styleUrl: './dashboard-page.scss',
})
export class DashboardPage {
  private readonly dashboardService = inject(DashboardService);
  private readonly authService = inject(AuthService);

  protected readonly trendMonths = TREND_MONTHS;
  protected readonly isAdmin = computed(() => this.authService.hasRole('Admin'));

  protected readonly stats = rxResource({ stream: () => this.dashboardService.getStats() });
  protected readonly gradesTrend = rxResource({
    stream: () => this.dashboardService.getGradesTrend(TREND_MONTHS),
  });
  protected readonly weeklyAttendance = rxResource({
    stream: () => this.dashboardService.getWeeklyAttendance(),
  });
  protected readonly gradeDistribution = rxResource({
    stream: () => this.dashboardService.getGradeDistribution(),
  });
  protected readonly topStudents = rxResource({
    stream: () => this.dashboardService.getTopStudents(),
  });
  protected readonly recentActivity = rxResource({
    stream: () => this.dashboardService.getRecentActivity(),
  });

  /** The latest month with grades, and how much it changed from the month before it. */
  protected readonly trendSummary = computed(() => {
    const graded = (this.gradesTrend.value() ?? []).filter((m) => m.averageScore !== null);
    const latest = graded.at(-1)?.averageScore ?? null;
    const previous = graded.at(-2)?.averageScore ?? null;
    return {
      latest,
      change:
        latest !== null && previous !== null ? Math.round((latest - previous) * 10) / 10 : null,
    };
  });

  /** The error message of a resource, in Hebrew, or null. */
  protected errorOf(resource: { error(): unknown }): string | null {
    return resource.error() ? toErrorMessage(resource.error()) : null;
  }
}
