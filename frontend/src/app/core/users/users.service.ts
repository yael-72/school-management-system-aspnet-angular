import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { PagedResult, toHttpParams } from '../api/api.models';
import {
  CreateUserRequest,
  Role,
  UpdateUserRequest,
  UserDetails,
  UserListItem,
  UserListQuery,
} from './users.models';

/** Staff members (/api/users and /api/roles – admin only). */
@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/users';

  getUsers(query: UserListQuery = {}): Observable<PagedResult<UserListItem>> {
    return this.http.get<PagedResult<UserListItem>>(this.baseUrl, { params: toHttpParams(query) });
  }

  /** Active staff who can be a homeroom teacher or get an assignment, sorted by name. */
  getActiveStaff(): Observable<UserListItem[]> {
    return this.getUsers({ isActive: true, pageSize: 100, sortBy: 'fullName' }).pipe(
      map((page) => page.items),
    );
  }

  getUser(id: number): Observable<UserDetails> {
    return this.http.get<UserDetails>(`${this.baseUrl}/${id}`);
  }

  createUser(request: CreateUserRequest): Observable<UserDetails> {
    return this.http.post<UserDetails>(this.baseUrl, request);
  }

  updateUser(id: number, request: UpdateUserRequest): Observable<UserDetails> {
    return this.http.put<UserDetails>(`${this.baseUrl}/${id}`, request);
  }

  /** A deactivated user cannot log in; their history is kept. */
  setStatus(id: number, isActive: boolean): Observable<UserDetails> {
    return this.http.patch<UserDetails>(`${this.baseUrl}/${id}/status`, { isActive });
  }

  /** Sets a new password for a user who forgot hers. */
  resetPassword(id: number, newPassword: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/password`, { newPassword });
  }

  deleteUser(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  getRoles(): Observable<Role[]> {
    return this.http.get<Role[]>('/api/roles');
  }
}
