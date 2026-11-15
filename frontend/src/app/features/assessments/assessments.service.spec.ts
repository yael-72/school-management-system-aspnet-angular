import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AssessmentsService } from './assessments.service';

describe('AssessmentsService', () => {
  let service: AssessmentsService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AssessmentsService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends the filters as query parameters and skips empty ones', () => {
    service.getAssessments({ classId: 3, type: 'Quiz', search: '', page: 2 }).subscribe();

    const request = http.expectOne((r) => r.url === '/api/assessments');
    expect(request.request.params.get('classId')).toBe('3');
    expect(request.request.params.get('type')).toBe('Quiz');
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.has('search')).toBe(false);
    request.flush({ items: [], totalCount: 0, page: 2, pageSize: 20 });
  });

  it('saves scores with PUT /api/assessments/{id}/scores', () => {
    const body = { scores: [{ studentId: 1, score: 92, comment: null }] };

    service.saveScores(7, body).subscribe();

    const request = http.expectOne('/api/assessments/7/scores');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(body);
    request.flush({});
  });

  it('deletes with DELETE /api/assessments/{id}', () => {
    service.deleteAssessment(7).subscribe();

    const request = http.expectOne('/api/assessments/7');
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });
});
