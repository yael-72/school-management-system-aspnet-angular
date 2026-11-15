import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { todayIsoDate } from '../../shared/utils/dates';
import { AttendanceRegister } from './attendance-register';
import { ClassAttendance, SaveAttendanceRequest } from './attendance.models';

describe('AttendanceRegister', () => {
  const today = todayIsoDate();
  const register: ClassAttendance = {
    classId: 3,
    className: "ז'1",
    date: today,
    students: [
      { studentId: 1, firstName: 'יוסף', lastName: 'אברהם', status: 'Absent', note: null },
      { studentId: 2, firstName: 'דוד', lastName: 'ברק', status: null, note: null },
      { studentId: 3, firstName: 'משה', lastName: 'גל', status: null, note: null },
    ],
  };

  let http: HttpTestingController;

  async function render() {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(AttendanceRegister);
    fixture.componentRef.setInput('classId', 3);
    fixture.detectChanges();
    http.expectOne((r) => r.url === '/api/attendance').flush(register);
    await fixture.whenStable();
    return { fixture, element: fixture.nativeElement as HTMLElement };
  }

  function button(element: HTMLElement, text: string): HTMLButtonElement {
    const found = Array.from(element.querySelectorAll('button')).find((b) =>
      b.textContent?.includes(text),
    );
    if (!found) throw new Error(`Button "${text}" not found`);
    return found;
  }

  afterEach(() => http.verify());

  it('shows a row per student and counts the unmarked ones', async () => {
    const { element } = await render();

    expect(element.querySelectorAll('tbody tr').length).toBe(3);
    expect(element.textContent).toContain('לא סומנו: 2');
    expect(button(element, 'שמירת נוכחות').disabled).toBe(true); // nothing changed yet
  });

  it('marks the remaining students as present and saves only marked rows', async () => {
    const { fixture, element } = await render();

    button(element, 'סימון כל השאר כנוכחים').click();
    await fixture.whenStable();
    button(element, 'שמירת נוכחות').click();

    const request = http.expectOne('/api/attendance');
    expect(request.request.method).toBe('PUT');
    expect((request.request.body as SaveAttendanceRequest).records).toEqual([
      { studentId: 1, status: 'Absent', note: null }, // already marked – left as it was
      { studentId: 2, status: 'Present', note: null },
      { studentId: 3, status: 'Present', note: null },
    ]);
    request.flush({
      ...register,
      students: register.students.map((s) => ({ ...s, status: s.status ?? 'Present' })),
    });
    await fixture.whenStable();

    expect(element.textContent).toContain('הנוכחות נשמרה');
    expect(button(element, 'שמירת נוכחות').disabled).toBe(true);
  });

  it('shows the API error in Hebrew when saving fails', async () => {
    const { fixture, element } = await render();
    const radio = element.querySelector<HTMLInputElement>('input[name="status-2"]')!;
    radio.click();
    await fixture.whenStable();

    button(element, 'שמירת נוכחות').click();
    http
      .expectOne('/api/attendance')
      .flush(
        { detail: 'Attendance cannot be recorded for a future date.' },
        { status: 400, statusText: 'Bad Request' },
      );
    await fixture.whenStable();

    expect(element.querySelector('[role="alert"]')?.textContent).toContain('לתאריך עתידי');
  });
});
