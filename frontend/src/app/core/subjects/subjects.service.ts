import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

/** SubjectDto in the API. */
export interface Subject {
  id: number;
  name: string;
}

/** School subjects (any logged-in user can read them). */
@Injectable({ providedIn: 'root' })
export class SubjectsService {
  private readonly http = inject(HttpClient);

  /** All subjects, alphabetically. */
  getSubjects(): Observable<Subject[]> {
    return this.http.get<Subject[]>('/api/subjects');
  }
}
