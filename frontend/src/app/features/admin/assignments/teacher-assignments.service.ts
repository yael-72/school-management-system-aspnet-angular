import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedResult, toHttpParams } from '../../../core/api/api.models';
import {
  CreateTeacherAssignmentRequest,
  TeacherAssignment,
  TeacherAssignmentListQuery,
} from './teacher-assignments.models';

/** Who teaches which subject in which class (/api/teacher-assignments – admin only). */
@Injectable({ providedIn: 'root' })
export class TeacherAssignmentsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/teacher-assignments';

  getAssignments(
    query: TeacherAssignmentListQuery = {},
  ): Observable<PagedResult<TeacherAssignment>> {
    return this.http.get<PagedResult<TeacherAssignment>>(this.baseUrl, {
      params: toHttpParams(query),
    });
  }

  createAssignment(request: CreateTeacherAssignmentRequest): Observable<TeacherAssignment> {
    return this.http.post<TeacherAssignment>(this.baseUrl, request);
  }

  deleteAssignment(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
