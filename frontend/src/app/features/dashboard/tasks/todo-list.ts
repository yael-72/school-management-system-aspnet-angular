import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { toErrorMessage } from '../../../core/api/api-error';
import { EmptyState } from '../../../shared/components/empty-state/empty-state';
import { Skeleton } from '../../../shared/components/skeleton/skeleton';
import { todayIsoDate } from '../../../shared/utils/dates';
import { TodoTask } from './tasks.models';
import { TasksService } from './tasks.service';

/** The user's personal to-do list (Horizon "Tasks" card): add, tick off and delete tasks. */
@Component({
  selector: 'app-todo-list',
  imports: [DatePipe, EmptyState, ReactiveFormsModule, Skeleton],
  templateUrl: './todo-list.html',
  styleUrl: './todo-list.scss',
})
export class TodoList {
  private readonly tasksService = inject(TasksService);

  protected readonly today = todayIsoDate();

  protected readonly tasks = rxResource({ stream: () => this.tasksService.getTasks() });

  protected readonly openCount = computed(
    () => (this.tasks.value() ?? []).filter((task) => !task.isDone).length,
  );

  protected readonly loadError = computed(() =>
    this.tasks.error() ? toErrorMessage(this.tasks.error()) : null,
  );

  protected readonly actionError = signal<string | null>(null);
  protected readonly busy = signal(false);

  protected readonly form = inject(NonNullableFormBuilder).group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    dueDate: [''],
  });

  protected isOverdue(task: TodoTask): boolean {
    return !task.isDone && task.dueDate !== null && task.dueDate < this.today;
  }

  protected add(): void {
    const title = this.form.controls.title.value.trim();
    if (!title) {
      return;
    }
    const dueDate = this.form.controls.dueDate.value || null;
    this.run(this.tasksService.createTask({ title, dueDate, isDone: false }), () =>
      this.form.reset(),
    );
  }

  protected toggle(task: TodoTask): void {
    this.run(this.tasksService.toggleTask(task.id));
  }

  protected remove(task: TodoTask): void {
    this.run(this.tasksService.deleteTask(task.id));
  }

  /** Sends one change, then reloads the list so its order (open first, by due date) stays correct. */
  private run(request$: Observable<unknown>, onSuccess?: () => void): void {
    this.busy.set(true);
    this.actionError.set(null);
    request$.subscribe({
      next: () => {
        this.busy.set(false);
        onSuccess?.();
        this.tasks.reload();
      },
      error: (error: unknown) => {
        this.busy.set(false);
        this.actionError.set(toErrorMessage(error));
      },
    });
  }
}
