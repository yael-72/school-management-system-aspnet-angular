import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

/** SubjectDto in the API. */
export interface Subject {
  id: number;
  name: string;
}

/** School subjects (any logged-in user can read them; only admins change them). */
@Injectable({ providedIn: 'root' })
export class SubjectsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/subjects';

  /** All subjects, alphabetically. */
  getSubjects(): Observable<Subject[]> {
    return this.http.get<Subject[]>(this.baseUrl);
  }

  createSubject(name: string): Observable<Subject> {
    return this.http.post<Subject>(this.baseUrl, { name });
  }

  renameSubject(id: number, name: string): Observable<Subject> {
    return this.http.put<Subject>(`${this.baseUrl}/${id}`, { name });
  }

  /** Only a subject that is not taught and has no assessments can be deleted. */
  deleteSubject(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
