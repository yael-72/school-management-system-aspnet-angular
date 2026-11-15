import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { DashboardService } from './dashboard.service';

describe('DashboardService', () => {
  let service: DashboardService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(DashboardService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('asks for the number of months of the grades trend', () => {
    service.getGradesTrend(6).subscribe();

    const request = http.expectOne((r) => r.url === '/api/dashboard/grades-trend');
    expect(request.request.params.get('months')).toBe('6');
    request.flush([]);
  });

  it('asks for the number of top students', () => {
    service.getTopStudents(5).subscribe();

    const request = http.expectOne((r) => r.url === '/api/dashboard/top-students');
    expect(request.request.params.get('count')).toBe('5');
    request.flush([]);
  });
});
