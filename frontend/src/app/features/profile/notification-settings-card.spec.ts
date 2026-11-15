import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { NotificationSettingsCard } from './notification-settings-card';

describe('NotificationSettingsCard', () => {
  const url = '/api/users/me/notification-settings';
  let http: HttpTestingController;

  async function render() {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(NotificationSettingsCard);
    fixture.detectChanges();
    http
      .expectOne(url)
      .flush({ remindBeforeExam: true, alertOnLowAttendance: true, weeklySummaryEmail: false });
    await fixture.whenStable();
    const switches = () =>
      Array.from(
        (fixture.nativeElement as HTMLElement).querySelectorAll<HTMLInputElement>(
          'input[role="switch"]',
        ),
      );
    return { fixture, switches };
  }

  afterEach(() => http.verify());

  it('saves a change right away', async () => {
    const { fixture, switches } = await render();

    switches()[2].click();
    const save = http.expectOne(url);
    expect(save.request.method).toBe('PUT');
    expect(save.request.body).toEqual({
      remindBeforeExam: true,
      alertOnLowAttendance: true,
      weeklySummaryEmail: true,
    });
    save.flush(save.request.body);
    await fixture.whenStable();

    expect(switches()[2].checked).toBe(true);
  });

  it('moves the switch back and shows a message when saving fails', async () => {
    const { fixture, switches } = await render();

    switches()[0].click();
    http.expectOne(url).flush(null, { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();

    expect(switches()[0].checked).toBe(true);
    expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')).not.toBeNull();
  });
});
