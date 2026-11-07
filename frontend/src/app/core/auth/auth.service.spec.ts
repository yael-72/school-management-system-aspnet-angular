import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { LoginResponse } from './auth.models';
import { AuthService } from './auth.service';

const inOneHour = () => new Date(Date.now() + 60 * 60 * 1000).toISOString();

const response = (): LoginResponse => ({
  accessToken: 'token-123',
  expiresAt: inOneHour(),
  user: { id: 1, fullName: 'משה כהן', email: 'teacher@school.demo', role: 'Teacher' },
});

describe('AuthService', () => {
  let http: HttpTestingController;

  function createService(): AuthService {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([{ path: '**', children: [] }])],
    });
    http = TestBed.inject(HttpTestingController);
    return TestBed.inject(AuthService);
  }

  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
  });

  it('logs in, stores the session for this tab only, and exposes the user', () => {
    const service = createService();

    service.login({ email: 'teacher@school.demo', password: 'x', rememberMe: false }).subscribe();
    http.expectOne('/api/auth/login').flush(response());

    expect(service.isAuthenticated()).toBe(true);
    expect(service.currentUser()?.fullName).toBe('משה כהן');
    expect(service.getAccessToken()).toBe('token-123');
    expect(sessionStorage.getItem('auth_session')).not.toBeNull();
    expect(localStorage.getItem('auth_session')).toBeNull();
  });

  it('keeps the session in localStorage when "keep me logged in" is ticked', () => {
    const service = createService();

    service.login({ email: 'teacher@school.demo', password: 'x', rememberMe: true }).subscribe();
    http.expectOne('/api/auth/login').flush(response());

    expect(localStorage.getItem('auth_session')).not.toBeNull();
  });

  it('restores a saved session on startup', () => {
    localStorage.setItem('auth_session', JSON.stringify(response()));

    const service = createService();

    expect(service.isAuthenticated()).toBe(true);
    expect(service.hasRole('Teacher')).toBe(true);
    expect(service.hasRole('Admin')).toBe(false);
  });

  it('ignores an expired saved session', () => {
    localStorage.setItem('auth_session', JSON.stringify({ ...response(), expiresAt: '2020-01-01T00:00:00Z' }));

    const service = createService();

    expect(service.isAuthenticated()).toBe(false);
    expect(service.getAccessToken()).toBeNull();
  });

  it('logout clears the session', () => {
    localStorage.setItem('auth_session', JSON.stringify(response()));
    const service = createService();

    service.logout();

    expect(service.isAuthenticated()).toBe(false);
    expect(localStorage.getItem('auth_session')).toBeNull();
  });
});
