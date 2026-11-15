import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { LoginPage } from './login-page';

describe('LoginPage', () => {
  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
  });

  async function render() {
    const fixture = TestBed.createComponent(LoginPage);
    await fixture.whenStable();
    return { fixture, element: fixture.nativeElement as HTMLElement };
  }

  function type(element: HTMLElement, selector: string, value: string) {
    const input = element.querySelector<HTMLInputElement>(selector)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  it('shows validation messages and sends nothing when the form is empty', async () => {
    const { fixture, element } = await render();

    element.querySelector<HTMLButtonElement>('button[type=submit]')!.click();
    await fixture.whenStable();

    expect(element.textContent).toContain('יש להזין אימייל');
    expect(element.textContent).toContain('יש להזין סיסמה');
    TestBed.inject(HttpTestingController).expectNone('/api/auth/login');
  });

  it('shows a friendly message when the password is wrong', async () => {
    const { fixture, element } = await render();
    type(element, '#email', 'admin@school.demo');
    type(element, '#password', 'wrong');

    element.querySelector<HTMLButtonElement>('button[type=submit]')!.click();
    TestBed.inject(HttpTestingController)
      .expectOne('/api/auth/login')
      .flush({ detail: 'Invalid email or password.' }, { status: 401, statusText: 'Unauthorized' });
    await fixture.whenStable();

    expect(element.querySelector('[role=alert]')?.textContent).toContain('אימייל או סיסמה שגויים');
  });

  it('goes to the dashboard after a successful login', async () => {
    const { fixture, element } = await render();
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    element.querySelector<HTMLButtonElement>('.auth__demo')!.click();

    element.querySelector<HTMLButtonElement>('button[type=submit]')!.click();
    TestBed.inject(HttpTestingController)
      .expectOne('/api/auth/login')
      .flush({
        accessToken: 't',
        expiresAt: new Date(Date.now() + 60_000).toISOString(),
        user: { id: 1, fullName: 'אהרן אברמסון', email: 'admin@school.demo', role: 'Admin' },
      });
    await fixture.whenStable();

    expect(navigate).toHaveBeenCalledWith('/dashboard');
  });
});
