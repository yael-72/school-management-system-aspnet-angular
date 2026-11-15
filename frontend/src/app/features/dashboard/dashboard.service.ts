import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { toHttpParams } from '../../core/api/api.models';
import {
  DailyAttendance,
  DashboardStats,
  GradeDistribution,
  MonthlyAverage,
  RecentActivity,
  TopStudent,
} from './dashboard.models';

/** Calls the /api/dashboard endpoints. The API scopes every number by the user's role. */
@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/dashboard';

  getStats(): Observable<DashboardStats> {
    return this.http.get<DashboardStats>(`${this.baseUrl}/stats`);
  }

  getGradesTrend(months = 6): Observable<MonthlyAverage[]> {
    return this.http.get<MonthlyAverage[]>(`${this.baseUrl}/grades-trend`, {
      params: toHttpParams({ months }),
    });
  }

  getWeeklyAttendance(): Observable<DailyAttendance[]> {
    return this.http.get<DailyAttendance[]>(`${this.baseUrl}/attendance-weekly`);
  }

  getGradeDistribution(): Observable<GradeDistribution> {
    return this.http.get<GradeDistribution>(`${this.baseUrl}/grade-distribution`);
  }

  getTopStudents(count = 5): Observable<TopStudent[]> {
    return this.http.get<TopStudent[]>(`${this.baseUrl}/top-students`, {
      params: toHttpParams({ count }),
    });
  }

  getRecentActivity(count = 6): Observable<RecentActivity[]> {
    return this.http.get<RecentActivity[]>(`${this.baseUrl}/recent-activity`, {
      params: toHttpParams({ count }),
    });
  }
}
