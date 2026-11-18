import { Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { toErrorMessage } from '../../../core/api/api-error';
import { Subject, SubjectsService } from '../../../core/subjects/subjects.service';
import { Dialog } from '../../../shared/components/dialog/dialog';
import { EmptyState } from '../../../shared/components/empty-state/empty-state';
import { Skeleton } from '../../../shared/components/skeleton/skeleton';

/**
 * /admin/subjects – a short list, so everything happens inline: add at the top, rename in place,
 * delete with a confirmation. The API refuses to delete a subject that is taught or has exams.
 */
@Component({
  selector: 'app-subjects-admin-page',
  imports: [Dialog, EmptyState, ReactiveFormsModule, Skeleton],
  templateUrl: './subjects-admin-page.html',
  styleUrl: './subjects-admin-page.scss',
})
export class SubjectsAdminPage {
  private readonly subjectsService = inject(SubjectsService);
  private readonly formBuilder = inject(NonNullableFormBuilder);

  protected readonly subjects = rxResource({ stream: () => this.subjectsService.getSubjects() });

  protected readonly loadError = computed(() =>
    this.subjects.error() ? toErrorMessage(this.subjects.error()) : null,
  );

  // Form groups (not lone controls) so that (ngSubmit) works: it comes from [formGroup].
  protected readonly addForm = this.formBuilder.group({
    name: ['', [Validators.required, Validators.maxLength(50)]],
  });
  protected readonly editForm = this.formBuilder.group({
    name: ['', [Validators.required, Validators.maxLength(50)]],
  });

  protected readonly editingId = signal<number | null>(null);
  protected readonly subjectToDelete = signal<Subject | null>(null);
  protected readonly busy = signal(false);
  protected readonly actionError = signal<string | null>(null);

  protected add(): void {
    const name = this.addForm.controls.name.value.trim();
    if (!name || this.addForm.invalid) {
      this.addForm.markAllAsTouched();
      return;
    }
    this.run(this.subjectsService.createSubject(name), () => this.addForm.reset());
  }

  protected startEdit(subject: Subject): void {
    this.actionError.set(null);
    this.editForm.reset({ name: subject.name });
    this.editingId.set(subject.id);
  }

  protected saveEdit(subject: Subject): void {
    const name = this.editForm.controls.name.value.trim();
    if (!name || this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }
    if (name === subject.name) {
      this.editingId.set(null);
      return;
    }
    this.run(this.subjectsService.renameSubject(subject.id, name), () => this.editingId.set(null));
  }

  protected askDelete(subject: Subject): void {
    this.actionError.set(null);
    this.subjectToDelete.set(subject);
  }

  protected confirmDelete(): void {
    const subject = this.subjectToDelete();
    if (subject) {
      this.run(
        this.subjectsService.deleteSubject(subject.id),
        () => this.subjectToDelete.set(null),
        () => this.subjectToDelete.set(null),
      );
    }
  }

  /** Sends one change, then reloads the list (it is sorted by name on the server). */
  private run(request$: Observable<unknown>, onSuccess: () => void, onError?: () => void): void {
    this.busy.set(true);
    this.actionError.set(null);
    request$.subscribe({
      next: () => {
        this.busy.set(false);
        onSuccess();
        this.subjects.reload();
      },
      error: (error: unknown) => {
        this.busy.set(false);
        onError?.();
        this.actionError.set(toErrorMessage(error));
      },
    });
  }
}
