import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AttendanceService } from './attendance.service';

describe('AttendanceService', () => {
  let service: AttendanceService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AttendanceService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads the register of a class on a date', () => {
    service.getClassAttendance(3, '2026-11-10').subscribe();

    const request = http.expectOne((r) => r.url === '/api/attendance');
    expect(request.request.params.get('classId')).toBe('3');
    expect(request.request.params.get('date')).toBe('2026-11-10');
    request.flush({ classId: 3, className: "ז'1", date: '2026-11-10', students: [] });
  });

  it('saves with PUT /api/attendance', () => {
    const body = {
      classId: 3,
      date: '2026-11-10',
      records: [{ studentId: 1, status: 'Late' as const, note: null }],
    };

    service.saveClassAttendance(body).subscribe();

    const request = http.expectOne('/api/attendance');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(body);
    request.flush({ classId: 3, className: "ז'1", date: '2026-11-10', students: [] });
  });

  it('loads a report for a date range', () => {
    service.getReport(3, '2026-11-01', '2026-11-10').subscribe();

    const request = http.expectOne((r) => r.url === '/api/attendance/report');
    expect(request.request.params.get('from')).toBe('2026-11-01');
    expect(request.request.params.get('to')).toBe('2026-11-10');
    request.flush({
      classId: 3,
      className: "ז'1",
      from: '',
      to: '',
      attendanceRate: null,
      students: [],
    });
  });
});
