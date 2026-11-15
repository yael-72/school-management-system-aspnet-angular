import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedResult, toHttpParams } from '../../core/api/api.models';
import {
  Assessment,
  AssessmentListItem,
  AssessmentListQuery,
  AssessmentScores,
  SaveAssessmentRequest,
  SaveScoresRequest,
} from './assessments.models';

/** Calls the /api/assessments endpoints. */
@Injectable({ providedIn: 'root' })
export class AssessmentsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/assessments';

  getAssessments(query: AssessmentListQuery = {}): Observable<PagedResult<AssessmentListItem>> {
    return this.http.get<PagedResult<AssessmentListItem>>(this.baseUrl, {
      params: toHttpParams(query),
    });
  }

  getAssessment(id: number): Observable<Assessment> {
    return this.http.get<Assessment>(`${this.baseUrl}/${id}`);
  }

  createAssessment(request: SaveAssessmentRequest): Observable<Assessment> {
    return this.http.post<Assessment>(this.baseUrl, request);
  }

  updateAssessment(id: number, request: SaveAssessmentRequest): Observable<Assessment> {
    return this.http.put<Assessment>(`${this.baseUrl}/${id}`, request);
  }

  /** Deletes the assessment together with all its scores. */
  deleteAssessment(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  getScores(id: number): Observable<AssessmentScores> {
    return this.http.get<AssessmentScores>(`${this.baseUrl}/${id}/scores`);
  }

  /** Bulk upsert of the class's scores on this assessment. */
  saveScores(id: number, request: SaveScoresRequest): Observable<AssessmentScores> {
    return this.http.put<AssessmentScores>(`${this.baseUrl}/${id}/scores`, request);
  }
}
