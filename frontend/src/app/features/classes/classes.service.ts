import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedResult, toHttpParams } from '../../core/api/api.models';
import { ClassDetails, ClassListQuery, ClassSummary, SaveClassRequest } from './classes.models';

/** Calls the /api/classes endpoints. Components never use HttpClient directly. */
@Injectable({ providedIn: 'root' })
export class ClassesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/classes';

  getClasses(query: ClassListQuery = {}): Observable<PagedResult<ClassSummary>> {
    return this.http.get<PagedResult<ClassSummary>>(this.baseUrl, { params: toHttpParams(query) });
  }

  getClass(id: number): Observable<ClassDetails> {
    return this.http.get<ClassDetails>(`${this.baseUrl}/${id}`);
  }

  createClass(request: SaveClassRequest): Observable<ClassDetails> {
    return this.http.post<ClassDetails>(this.baseUrl, request);
  }

  updateClass(id: number, request: SaveClassRequest): Observable<ClassDetails> {
    return this.http.put<ClassDetails>(`${this.baseUrl}/${id}`, request);
  }

  deleteClass(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
