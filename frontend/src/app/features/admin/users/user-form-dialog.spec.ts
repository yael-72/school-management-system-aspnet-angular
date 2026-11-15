import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { UserFormDialog } from './user-form-dialog';

describe('UserFormDialog', () => {
  const roles = [
    { id: 1, name: 'Admin' },
    { id: 2, name: 'Teacher' },
  ];
  let http: HttpTestingController;

  // jsdom has no <dialog> methods; a minimal stand-in is enough for these tests.
  beforeAll(() => {
    HTMLDialogElement.prototype.showModal ??= function (this: HTMLDialogElement) {
      this.setAttribute('open', '');
    };
    HTMLDialogElement.prototype.close ??= function (this: HTMLDialogElement) {
      this.removeAttribute('open');
    };
  });

  async function render(userId: number | null, currentUserId = 1) {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    const auth = TestBed.inject(AuthService);
    Object.defineProperty(auth, 'currentUser', {
      value: () => ({
        id: currentUserId,
        fullName: 'מנהל',
        email: 'admin@school.demo',
        role: 'Admin',
      }),
    });

    const fixture = TestBed.createComponent(UserFormDialog);
    fixture.componentRef.setInput('userId', userId);
    fixture.componentRef.setInput('open', true);
    fixture.detectChanges();
    http.expectOne('/api/roles').flush(roles);
    if (userId !== null) {
      http.expectOne(`/api/users/${userId}`).flush({
        id: userId,
        fullName: 'משה לוינסון',
        email: 'teacher@school.demo',
        phone: null,
        roleId: 2,
        role: 'Teacher',
        isActive: true,
        createdAt: '',
      });
    }
    await fixture.whenStable();
    return { fixture, element: fixture.nativeElement as HTMLElement };
  }

  function type(element: HTMLElement, selector: string, value: string): void {
    const input = element.querySelector<HTMLInputElement>(selector)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  /** Picks an option by its visible text, as a user would. */
  function choose(element: HTMLElement, selector: string, text: string): void {
    const select = element.querySelector<HTMLSelectElement>(selector)!;
    select.selectedIndex = Array.from(select.options).findIndex((o) => o.text.trim() === text);
    select.dispatchEvent(new Event('change'));
  }

  afterEach(() => http.verify());

  it('creates a user with a first password and a trimmed name', async () => {
    const { fixture, element } = await render(null);
    type(element, '#user-name', '  דוד הירש ');
    type(element, '#user-email', 'david@school.demo');
    choose(element, '#user-role', 'מורה');
    type(element, '#user-password', 'Welcome123');
    await fixture.whenStable();

    element.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();

    const request = http.expectOne((r) => r.url === '/api/users' && r.method === 'POST');
    expect(request.request.body).toEqual({
      fullName: 'דוד הירש',
      email: 'david@school.demo',
      phone: null,
      roleId: 2,
      password: 'Welcome123',
    });
    request.flush({});
  });

  it('does not send a weak password', async () => {
    const { fixture, element } = await render(null);
    type(element, '#user-name', 'דוד הירש');
    type(element, '#user-email', 'david@school.demo');
    choose(element, '#user-role', 'מורה');
    type(element, '#user-password', '12345678');
    await fixture.whenStable();

    element.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
    await fixture.whenStable();

    http.expectNone('/api/users');
    expect(element.textContent).toContain('אות וספרה');
  });

  it('has no password field when editing, and locks the role of yourself', async () => {
    const { element } = await render(5, 5);

    expect(element.querySelector('#user-password')).toBeNull();
    expect(element.querySelector<HTMLSelectElement>('#user-role')!.disabled).toBe(true);
    expect(element.querySelector<HTMLInputElement>('#user-name')!.value).toBe('משה לוינסון');
  });
});
