import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TeacherAssignmentsService } from './teacher-assignments.service';

describe('TeacherAssignmentsService', () => {
  let service: TeacherAssignmentsService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(TeacherAssignmentsService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends only the filters that are set', () => {
    service.getAssignments({ teacherId: 2, classId: undefined, sortBy: 'subjectName' }).subscribe();

    const request = http.expectOne((r) => r.url === '/api/teacher-assignments');
    expect(request.request.params.get('teacherId')).toBe('2');
    expect(request.request.params.has('classId')).toBe(false);
    expect(request.request.params.get('sortBy')).toBe('subjectName');
    request.flush({ items: [], totalCount: 0, page: 1, pageSize: 20 });
  });

  it('creates and deletes assignments', () => {
    service.createAssignment({ teacherId: 2, classId: 3, subjectId: 1 }).subscribe();
    service.deleteAssignment(7).subscribe();

    expect(http.expectOne((r) => r.method === 'POST').request.body).toEqual({
      teacherId: 2,
      classId: 3,
      subjectId: 1,
    });
    http
      .expectOne((r) => r.method === 'DELETE' && r.url === '/api/teacher-assignments/7')
      .flush(null);
  });
});
