import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  ChangePasswordRequest,
  NotificationSettings,
  Profile,
  UpdateProfileRequest,
} from './profile.models';

/** The logged-in user's own profile (/api/users/me). */
@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/users/me';

  getProfile(): Observable<Profile> {
    return this.http.get<Profile>(this.baseUrl);
  }

  updateProfile(request: UpdateProfileRequest): Observable<Profile> {
    return this.http.put<Profile>(this.baseUrl, request);
  }

  /** A wrong current password comes back as 400 (the user stays logged in). */
  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/password`, request);
  }

  getNotificationSettings(): Observable<NotificationSettings> {
    return this.http.get<NotificationSettings>(`${this.baseUrl}/notification-settings`);
  }

  updateNotificationSettings(settings: NotificationSettings): Observable<NotificationSettings> {
    return this.http.put<NotificationSettings>(`${this.baseUrl}/notification-settings`, settings);
  }
}
