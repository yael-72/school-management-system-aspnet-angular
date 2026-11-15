import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { UserRole } from './auth.models';
import { authGuard, guestGuard, roleGuard } from './auth.guards';
import { AuthService } from './auth.service';

describe('auth guards', () => {
  let loggedIn: boolean;
  let role: UserRole;

  const route = {} as ActivatedRouteSnapshot;
  const state = { url: '/students' } as RouterStateSnapshot;
  const urlOf = (result: unknown) => TestBed.inject(Router).serializeUrl(result as UrlTree);

  beforeEach(() => {
    loggedIn = false;
    role = 'Teacher';
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: { isAuthenticated: () => loggedIn, hasRole: (...roles: UserRole[]) => roles.includes(role) },
        },
      ],
    });
  });

  it('authGuard sends logged-out users to login with a return URL', () => {
    const result = TestBed.runInInjectionContext(() => authGuard(route, state));

    expect(urlOf(result)).toBe('/login?returnUrl=%2Fstudents');
  });

  it('authGuard lets logged-in users through', () => {
    loggedIn = true;

    expect(TestBed.runInInjectionContext(() => authGuard(route, state))).toBe(true);
  });

  it('guestGuard sends logged-in users to the dashboard', () => {
    loggedIn = true;

    expect(urlOf(TestBed.runInInjectionContext(() => guestGuard(route, state)))).toBe('/dashboard');
  });

  it('roleGuard blocks users without the role', () => {
    expect(urlOf(TestBed.runInInjectionContext(() => roleGuard('Admin')(route, state)))).toBe('/dashboard');

    role = 'Admin';
    expect(TestBed.runInInjectionContext(() => roleGuard('Admin')(route, state))).toBe(true);
  });
});
