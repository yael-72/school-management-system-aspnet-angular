import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { UserRole } from './auth.models';
import { AuthService } from './auth.service';

/** Only logged-in users; others go to the login page and return here afterwards. */
export const authGuard: CanActivateFn = (_route, state) => {
  const authService = inject(AuthService);
  if (authService.isAuthenticated()) {
    return true;
  }
  return inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

/** The login page is only for logged-out users; logged-in users go to the dashboard. */
export const guestGuard: CanActivateFn = () => {
  return inject(AuthService).isAuthenticated() ? inject(Router).createUrlTree(['/dashboard']) : true;
};

/**
 * Only users with one of the given roles. Usage: canActivate: [roleGuard('Admin')].
 * This only hides screens; the API enforces the same rules on every request.
 */
export function roleGuard(...roles: UserRole[]): CanActivateFn {
  return () => (inject(AuthService).hasRole(...roles) ? true : inject(Router).createUrlTree(['/dashboard']));
}
