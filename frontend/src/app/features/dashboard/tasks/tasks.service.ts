import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { SaveTodoTaskRequest, TodoTask } from './tasks.models';

/** The current user's personal to-do list (/api/tasks). */
@Injectable({ providedIn: 'root' })
export class TasksService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/tasks';

  getTasks(): Observable<TodoTask[]> {
    return this.http.get<TodoTask[]>(this.baseUrl);
  }

  createTask(request: SaveTodoTaskRequest): Observable<TodoTask> {
    return this.http.post<TodoTask>(this.baseUrl, request);
  }

  toggleTask(id: number): Observable<TodoTask> {
    return this.http.patch<TodoTask>(`${this.baseUrl}/${id}/toggle`, null);
  }

  deleteTask(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
