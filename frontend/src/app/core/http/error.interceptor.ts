import { HttpErrorResponse, HttpInterceptorFn, HttpStatusCode } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';

/**
 * When the API answers 401 (token missing, expired or the user was disabled), log out and send
 * the user to the login page, so they can come back to the same page after logging in.
 * The login request itself is skipped: its 401 means "wrong password" and is shown on the form.
 */
export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return next(request).pipe(
    catchError((error: unknown) => {
      const isLoginRequest = request.url.endsWith('/api/auth/login');
      if (error instanceof HttpErrorResponse && error.status === HttpStatusCode.Unauthorized && !isLoginRequest) {
        authService.logout(router.url);
      }
      return throwError(() => error);
    }),
  );
};
