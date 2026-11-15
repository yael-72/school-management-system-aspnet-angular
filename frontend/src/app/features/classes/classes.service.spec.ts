import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ClassesService } from './classes.service';

describe('ClassesService', () => {
  let service: ClassesService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ClassesService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends the grade filter and paging as query parameters', () => {
    service.getClasses({ gradeLevel: 7, page: 1, pageSize: 100 }).subscribe();

    const request = http.expectOne((r) => r.url === '/api/classes');
    expect(request.request.params.toString()).toBe('gradeLevel=7&page=1&pageSize=100');
    request.flush({ items: [], totalCount: 0, page: 1, pageSize: 100 });
  });

  it('updates a class with PUT', () => {
    const body = { name: "ז'1", gradeLevel: 7, schoolYear: '2026-2027', homeroomTeacherId: 2 };

    service.updateClass(5, body).subscribe();

    const request = http.expectOne('/api/classes/5');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(body);
    request.flush({});
  });
});
