import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { toHttpParams } from '../../../core/api/api.models';
import { SaveEventRequest, SchoolEvent } from './events.models';

/** The school calendar (/api/events). */
@Injectable({ providedIn: 'root' })
export class EventsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/events';

  /** Events that overlap the range (both dates included). */
  getEvents(from: string, to: string): Observable<SchoolEvent[]> {
    return this.http.get<SchoolEvent[]>(this.baseUrl, { params: toHttpParams({ from, to }) });
  }

  createEvent(request: SaveEventRequest): Observable<SchoolEvent> {
    return this.http.post<SchoolEvent>(this.baseUrl, request);
  }

  deleteEvent(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
