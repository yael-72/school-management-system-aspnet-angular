import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TodoList } from './todo-list';
import { TodoTask } from './tasks.models';

describe('TodoList', () => {
  const tasks: TodoTask[] = [
    { id: 1, title: 'לבדוק מבחנים', dueDate: '2000-01-01', isDone: false, createdAt: '' },
    { id: 2, title: 'להכין דף עבודה', dueDate: null, isDone: true, createdAt: '' },
  ];

  let http: HttpTestingController;

  async function render() {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(TodoList);
    fixture.detectChanges();
    http.expectOne('/api/tasks').flush(tasks);
    await fixture.whenStable();
    return { fixture, element: fixture.nativeElement as HTMLElement };
  }

  afterEach(() => http.verify());

  it('shows the tasks, marks overdue ones and counts the open ones', async () => {
    const { element } = await render();

    expect(element.querySelectorAll('.todo-item').length).toBe(2);
    expect(element.querySelector('.is-overdue')).not.toBeNull();
    expect(element.textContent).toContain('1 פתוחות');
  });

  it('toggles a task and reloads the list', async () => {
    const { fixture, element } = await render();

    element.querySelector<HTMLInputElement>('.todo-item input[type="checkbox"]')!.click();
    const toggle = http.expectOne('/api/tasks/1/toggle');
    expect(toggle.request.method).toBe('PATCH');
    toggle.flush({ ...tasks[0], isDone: true });
    TestBed.tick(); // lets the list start reloading

    http.expectOne('/api/tasks').flush(tasks.map((t) => ({ ...t, isDone: true })));
    await fixture.whenStable();
    expect(element.textContent).not.toContain('פתוחות');
  });

  it('adds a task with a trimmed title', async () => {
    const { fixture, element } = await render();
    const input = element.querySelector<HTMLInputElement>('input[formcontrolname="title"]')!;

    input.value = '  משימה חדשה  ';
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();
    element.querySelector<HTMLButtonElement>('.todo-form__add')!.click();

    const create = http.expectOne((r) => r.url === '/api/tasks' && r.method === 'POST');
    expect(create.request.body).toEqual({ title: 'משימה חדשה', dueDate: null, isDone: false });
    create.flush({ id: 3, title: 'משימה חדשה', dueDate: null, isDone: false, createdAt: '' });
    TestBed.tick();
    http.expectOne('/api/tasks').flush(tasks);
    await fixture.whenStable();

    expect(input.value).toBe(''); // the form is cleared after adding
  });
});
