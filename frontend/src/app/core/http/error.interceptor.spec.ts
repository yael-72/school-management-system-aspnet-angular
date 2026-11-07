import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { errorInterceptor } from './error.interceptor';

describe('errorInterceptor', () => {
  let http: HttpClient;
  let httpTesting: HttpTestingController;
  let logout: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    logout = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuthService, useValue: { logout } },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  it('logs out when the API answers 401', () => {
    http.get('/api/classes').subscribe({ error: () => undefined });
    httpTesting.expectOne('/api/classes').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(logout).toHaveBeenCalled();
  });

  it('does not log out on a failed login (wrong password)', () => {
    http.post('/api/auth/login', {}).subscribe({ error: () => undefined });
    httpTesting.expectOne('/api/auth/login').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(logout).not.toHaveBeenCalled();
  });

  it('does not log out on other errors', () => {
    http.get('/api/classes').subscribe({ error: () => undefined });
    httpTesting.expectOne('/api/classes').flush(null, { status: 500, statusText: 'Server Error' });

    expect(logout).not.toHaveBeenCalled();
  });
});
