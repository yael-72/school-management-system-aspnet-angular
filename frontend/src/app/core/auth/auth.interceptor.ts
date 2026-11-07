import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

/** Adds "Authorization: Bearer <token>" to every request to our API. */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const token = inject(AuthService).getAccessToken();

  // Only our own API gets the token, never a third-party URL.
  if (!token || !request.url.startsWith('/api/')) {
    return next(request);
  }

  return next(request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};
