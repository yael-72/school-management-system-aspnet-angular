import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { toHttpParams } from '../../core/api/api.models';
import { AttendanceReport, ClassAttendance, SaveAttendanceRequest } from './attendance.models';

/** Calls the /api/attendance endpoints. */
@Injectable({ providedIn: 'root' })
export class AttendanceService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/attendance';

  getClassAttendance(classId: number, date: string): Observable<ClassAttendance> {
    return this.http.get<ClassAttendance>(this.baseUrl, {
      params: toHttpParams({ classId, date }),
    });
  }

  /** Creates or updates the records of that day; the API returns the updated register. */
  saveClassAttendance(request: SaveAttendanceRequest): Observable<ClassAttendance> {
    return this.http.put<ClassAttendance>(this.baseUrl, request);
  }

  getReport(classId: number, from: string, to: string): Observable<AttendanceReport> {
    return this.http.get<AttendanceReport>(`${this.baseUrl}/report`, {
      params: toHttpParams({ classId, from, to }),
    });
  }
}
