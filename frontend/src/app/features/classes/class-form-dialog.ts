import { Component, effect, inject, input, output, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { toErrorMessage } from '../../core/api/api-error';
import { UsersService } from '../../core/users/users.service';
import { Dialog } from '../../shared/components/dialog/dialog';
import { ClassSummary, GRADE_LEVEL_NAMES, SaveClassRequest } from './classes.models';
import { ClassesService } from './classes.service';

/** "2026-2027" from September, "2025-2026" before that. */
export function currentSchoolYear(today = new Date()): string {
  const startYear = today.getMonth() >= 8 ? today.getFullYear() : today.getFullYear() - 1;
  return `${startYear}-${startYear + 1}`;
}

/**
 * Create or edit a class (admin only). Pass `schoolClass` to edit, or null to create a new one.
 */
@Component({
  selector: 'app-class-form-dialog',
  imports: [Dialog, ReactiveFormsModule],
  templateUrl: './class-form-dialog.html',
})
export class ClassFormDialog {
  private readonly classesService = inject(ClassesService);
  private readonly usersService = inject(UsersService);

  readonly open = input(false);
  readonly schoolClass = input<ClassSummary | null>(null);
  readonly saved = output<void>();
  readonly closed = output<void>();

  protected readonly gradeLevels = Object.entries(GRADE_LEVEL_NAMES).map(([value, name]) => ({
    value: Number(value),
    name,
  }));
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  /** Staff for the homeroom teacher list, loaded only while the dialog is open. */
  protected readonly staff = rxResource({
    params: () => (this.open() ? true : undefined),
    stream: () => this.usersService.getActiveStaff(),
  });

  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.maxLength(20)]],
    gradeLevel: [7, [Validators.required]],
    schoolYear: [currentSchoolYear(), [Validators.required, Validators.pattern(/^\d{4}-\d{4}$/)]],
    homeroomTeacherId: [null as number | null],
  });

  constructor() {
    // Each time the dialog opens, fill the form with the class being edited (or reset it for a new class).
    effect(() => {
      if (!this.open()) {
        return;
      }
      const schoolClass = this.schoolClass();
      this.errorMessage.set(null);
      this.form.reset({
        name: schoolClass?.name ?? '',
        gradeLevel: schoolClass?.gradeLevel ?? 7,
        schoolYear: schoolClass?.schoolYear ?? currentSchoolYear(),
        homeroomTeacherId: schoolClass?.homeroomTeacherId ?? null,
      });
    });
  }

  protected isInvalid(control: keyof typeof this.form.controls): boolean {
    const field = this.form.controls[control];
    return field.invalid && field.touched;
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request: SaveClassRequest = {
      ...this.form.getRawValue(),
      name: this.form.controls.name.value.trim(),
    };
    const existing = this.schoolClass();
    const save$ = existing
      ? this.classesService.updateClass(existing.id, request)
      : this.classesService.createClass(request);

    this.submitting.set(true);
    this.errorMessage.set(null);
    save$.subscribe({
      next: () => {
        this.submitting.set(false);
        this.saved.emit();
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.errorMessage.set(toErrorMessage(error));
      },
    });
  }
}
