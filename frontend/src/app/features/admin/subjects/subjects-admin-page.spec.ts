import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { SubjectsAdminPage } from './subjects-admin-page';

describe('SubjectsAdminPage', () => {
  let http: HttpTestingController;

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
    const fixture = TestBed.createComponent(SubjectsAdminPage);
    fixture.detectChanges();
    http.expectOne('/api/subjects').flush([{ id: 1, name: 'מתמטיקה' }]);
    await fixture.whenStable();
    return { fixture, element: fixture.nativeElement as HTMLElement };
  }

  function type(input: HTMLInputElement, value: string): void {
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  afterEach(() => http.verify());

  it('adds a subject when the form is submitted', async () => {
    const { element } = await render();
    type(element.querySelector<HTMLInputElement>('.add-form input')!, '  ספרות ');

    element.querySelector<HTMLButtonElement>('.add-form button[type="submit"]')!.click();

    const request = http.expectOne((r) => r.url === '/api/subjects' && r.method === 'POST');
    expect(request.request.body).toEqual({ name: 'ספרות' });
    request.flush({ id: 2, name: 'ספרות' });
    TestBed.tick(); // the resource reloads the list
    http.expectOne('/api/subjects').flush([]);
  });

  it('renames a subject in place', async () => {
    const { fixture, element } = await render();
    element.querySelector<HTMLButtonElement>('button[aria-label="שינוי שם מתמטיקה"]')!.click();
    fixture.detectChanges();
    type(element.querySelector<HTMLInputElement>('.subject-item__edit input')!, 'חשבון');

    element.querySelector<HTMLButtonElement>('.subject-item__edit button[type="submit"]')!.click();

    const request = http.expectOne((r) => r.url === '/api/subjects/1' && r.method === 'PUT');
    expect(request.request.body).toEqual({ name: 'חשבון' });
    request.flush({ id: 1, name: 'חשבון' });
    TestBed.tick(); // the resource reloads the list
    http.expectOne('/api/subjects').flush([]);
  });
});
