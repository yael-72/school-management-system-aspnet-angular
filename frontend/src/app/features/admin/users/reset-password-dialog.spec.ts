import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { UserListItem } from '../../../core/users/users.models';
import { ResetPasswordDialog } from './reset-password-dialog';

describe('ResetPasswordDialog', () => {
  const user: UserListItem = {
    id: 2,
    fullName: 'משה לוינסון',
    email: 'teacher@school.demo',
    role: 'Teacher',
    phone: null,
    isActive: true,
    assignmentCount: 3,
  };
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

  async function render() {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(ResetPasswordDialog);
    fixture.componentRef.setInput('user', user);
    fixture.detectChanges();
    await fixture.whenStable();
    return { fixture, element: fixture.nativeElement as HTMLElement };
  }

  function typePassword(element: HTMLElement, value: string): void {
    const input = element.querySelector<HTMLInputElement>('#reset-password')!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  afterEach(() => http.verify());

  it('sends the new password when the form is submitted', async () => {
    const { fixture, element } = await render();
    let done = false;
    fixture.componentInstance.done.subscribe(() => (done = true));
    typePassword(element, 'Reset2026x');

    element.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();

    const request = http.expectOne((r) => r.url === '/api/users/2/password' && r.method === 'PUT');
    expect(request.request.body).toEqual({ newPassword: 'Reset2026x' });
    request.flush(null, { status: 204, statusText: 'No Content' });
    expect(done).toBe(true);
  });

  it('does not send a weak password', async () => {
    const { fixture, element } = await render();
    typePassword(element, 'abcdefgh');

    element.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
    await fixture.whenStable();

    http.expectNone('/api/users/2/password');
    expect(element.querySelector('.form-error')).not.toBeNull();
  });
});
