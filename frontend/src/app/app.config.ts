import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { errorInterceptor } from './core/http/error.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    // withComponentInputBinding: query params such as ?returnUrl= arrive as component inputs.
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])),
  ],
};
