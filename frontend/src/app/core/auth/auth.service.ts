import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, map, tap } from 'rxjs';
import { CurrentUser, LoginRequest, LoginResponse, UserRole } from './auth.models';

const STORAGE_KEY = 'auth_session';

interface StoredSession {
  accessToken: string;
  expiresAt: string;
  user: CurrentUser;
}

/**
 * Logs the user in and out and keeps the current session in a signal.
 *
 * The session (JWT + user) is saved in localStorage when "keep me logged in" is ticked,
 * so it survives closing the browser, and in sessionStorage otherwise (cleared when the tab closes).
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly session = signal<StoredSession | null>(this.loadSession());

  readonly currentUser = computed(() => this.session()?.user ?? null);
  readonly isAuthenticated = computed(() => this.session() !== null);

  login(request: LoginRequest): Observable<CurrentUser> {
    return this.http.post<LoginResponse>('/api/auth/login', request).pipe(
      tap((response) => this.saveSession(response, request.rememberMe)),
      map((response) => response.user),
    );
  }

  /** Clears the session and goes to the login page (optionally coming back to `returnUrl` afterwards). */
  logout(returnUrl?: string): void {
    localStorage.removeItem(STORAGE_KEY);
    sessionStorage.removeItem(STORAGE_KEY);
    this.session.set(null);
    void this.router.navigate(['/login'], { queryParams: returnUrl ? { returnUrl } : {} });
  }

  /** The JWT to send to the API, or null when logged out or expired. */
  getAccessToken(): string | null {
    const session = this.session();
    if (session && isExpired(session)) {
      this.session.set(null);
      return null;
    }
    return session?.accessToken ?? null;
  }

  hasRole(...roles: UserRole[]): boolean {
    const user = this.currentUser();
    return user !== null && roles.includes(user.role);
  }

  private saveSession(response: LoginResponse, rememberMe: boolean): void {
    const session: StoredSession = {
      accessToken: response.accessToken,
      expiresAt: response.expiresAt,
      user: response.user,
    };
    const storage = rememberMe ? localStorage : sessionStorage;
    storage.setItem(STORAGE_KEY, JSON.stringify(session));
    this.session.set(session);
  }

  private loadSession(): StoredSession | null {
    const json = localStorage.getItem(STORAGE_KEY) ?? sessionStorage.getItem(STORAGE_KEY);
    if (!json) {
      return null;
    }
    try {
      const session = JSON.parse(json) as StoredSession;
      return isExpired(session) ? null : session;
    } catch {
      return null;
    }
  }
}

function isExpired(session: StoredSession): boolean {
  return new Date(session.expiresAt).getTime() <= Date.now();
}
