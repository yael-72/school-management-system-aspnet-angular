import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ChangePasswordDialog } from './change-password-dialog';

describe('ChangePasswordDialog', () => {
  let http: HttpTestingController;

  // jsdom has no <dialog> methods; a minimal stand-in is enough here.
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
    const fixture = TestBed.createComponent(ChangePasswordDialog);
    fixture.componentRef.setInput('open', true);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;

    const fill = async (current: string, next: string, confirm: string) => {
      for (const [id, value] of [
        ['#current-password', current],
        ['#new-password', next],
        ['#confirm-password', confirm],
      ]) {
        const input = element.querySelector<HTMLInputElement>(id)!;
        input.value = value;
        input.dispatchEvent(new Event('input'));
      }
      await fixture.whenStable();
      element.querySelector<HTMLButtonElement>('button[type="submit"]')!.click();
      await fixture.whenStable();
    };
    return { fixture, element, fill };
  }

  afterEach(() => http.verify());

  it('does not send when the confirmation does not match', async () => {
    const { element, fill } = await render();

    await fill('Demo123!', 'NewPassword1', 'NewPassword2');

    http.expectNone('/api/users/me/password');
    expect(element.textContent).toContain('הסיסמאות אינן תואמות');
  });

  it('sends the current and new password and reports success', async () => {
    const { fixture, fill } = await render();
    let changed = false;
    fixture.componentInstance.changed.subscribe(() => (changed = true));

    await fill('Demo123!', 'NewPassword1', 'NewPassword1');

    const request = http.expectOne('/api/users/me/password');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      currentPassword: 'Demo123!',
      newPassword: 'NewPassword1',
    });
    request.flush(null, { status: 204, statusText: 'No Content' });
    expect(changed).toBe(true);
  });

  it('shows the Hebrew message for a wrong current password', async () => {
    const { fixture, element, fill } = await render();

    await fill('Wrong1234', 'NewPassword1', 'NewPassword1');
    http
      .expectOne('/api/users/me/password')
      .flush(
        { detail: 'The current password is incorrect.' },
        { status: 400, statusText: 'Bad Request' },
      );
    await fixture.whenStable();

    expect(element.querySelector('[role="alert"]')?.textContent).toContain('הסיסמה הנוכחית שגויה');
  });
});
