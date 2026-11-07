import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { StudentsService } from './students.service';

describe('StudentsService', () => {
  let service: StudentsService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(StudentsService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends search, sorting and the class filter, and skips empty values', () => {
    service
      .getStudents({
        classId: 3,
        search: 'כהן',
        sortBy: 'lastName',
        sortDir: 'desc',
        page: 2,
        pageSize: 20,
      })
      .subscribe();

    const request = http.expectOne((r) => r.url === '/api/students');
    expect(request.request.params.get('classId')).toBe('3');
    expect(request.request.params.get('search')).toBe('כהן');
    expect(request.request.params.get('sortDir')).toBe('desc');
    request.flush({ items: [], totalCount: 0, page: 2, pageSize: 20 });

    service.getStudents({ search: '' }).subscribe();
    const second = http.expectOne((r) => r.url === '/api/students');
    expect(second.request.params.has('search')).toBe(false);
    second.flush({ items: [], totalCount: 0, page: 1, pageSize: 20 });
  });

  it('deletes with DELETE /api/students/{id}', () => {
    service.deleteStudent(7).subscribe();

    const request = http.expectOne('/api/students/7');
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });
});
