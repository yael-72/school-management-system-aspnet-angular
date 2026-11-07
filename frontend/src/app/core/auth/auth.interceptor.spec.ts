import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpTesting: HttpTestingController;
  let token: string | null;

  beforeEach(() => {
    token = 'token-123';
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: { getAccessToken: () => token } },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  it('adds the bearer token to API requests', () => {
    http.get('/api/classes').subscribe();

    expect(httpTesting.expectOne('/api/classes').request.headers.get('Authorization')).toBe('Bearer token-123');
  });

  it('does not send the token to other sites', () => {
    http.get('https://example.com/data').subscribe();

    expect(httpTesting.expectOne('https://example.com/data').request.headers.has('Authorization')).toBe(false);
  });

  it('sends no header when logged out', () => {
    token = null;
    http.get('/api/classes').subscribe();

    expect(httpTesting.expectOne('/api/classes').request.headers.has('Authorization')).toBe(false);
  });
});
