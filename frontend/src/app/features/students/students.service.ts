import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedResult, toHttpParams } from '../../core/api/api.models';
import {
  SaveStudentRequest,
  Student,
  StudentListItem,
  StudentListQuery,
  StudentSummary,
} from './students.models';

/** Calls the /api/students endpoints. */
@Injectable({ providedIn: 'root' })
export class StudentsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/students';

  getStudents(query: StudentListQuery = {}): Observable<PagedResult<StudentListItem>> {
    return this.http.get<PagedResult<StudentListItem>>(this.baseUrl, {
      params: toHttpParams(query),
    });
  }

  getStudent(id: number): Observable<Student> {
    return this.http.get<Student>(`${this.baseUrl}/${id}`);
  }

  getSummary(id: number): Observable<StudentSummary> {
    return this.http.get<StudentSummary>(`${this.baseUrl}/${id}/summary`);
  }

  createStudent(request: SaveStudentRequest): Observable<Student> {
    return this.http.post<Student>(this.baseUrl, request);
  }

  updateStudent(id: number, request: SaveStudentRequest): Observable<Student> {
    return this.http.put<Student>(`${this.baseUrl}/${id}`, request);
  }

  /** Soft delete on the server: the student disappears from lists but their history is kept. */
  deleteStudent(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
