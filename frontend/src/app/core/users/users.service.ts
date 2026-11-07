import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { PagedResult, toHttpParams } from '../api/api.models';
import { UserRole } from '../auth/auth.models';

export interface UserListItem {
  id: number;
  fullName: string;
  email: string;
  role: UserRole;
  phone: string | null;
  isActive: boolean;
}

/** Staff members (admin only). */
@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);

  /** Active staff who can be a homeroom teacher (teachers and the principal), sorted by name. */
  getActiveStaff(): Observable<UserListItem[]> {
    const params = toHttpParams({ isActive: true, pageSize: 100, sortBy: 'fullName' });
    return this.http
      .get<PagedResult<UserListItem>>('/api/users', { params })
      .pipe(map((page) => page.items));
  }
}
