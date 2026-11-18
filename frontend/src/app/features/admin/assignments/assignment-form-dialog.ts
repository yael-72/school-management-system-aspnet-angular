import { Component, effect, inject, input, output, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { toErrorMessage } from '../../../core/api/api-error';
import { SubjectsService } from '../../../core/subjects/subjects.service';
import { UsersService } from '../../../core/users/users.service';
import { Dialog } from '../../../shared/components/dialog/dialog';
import { ClassesService } from '../../classes/classes.service';
import { TeacherAssignment } from './teacher-assignments.models';
import { TeacherAssignmentsService } from './teacher-assignments.service';

/** "This teacher teaches this subject in this class." Pre-fills the filters of the list, if any. */
@Component({
  selector: 'app-assignment-form-dialog',
  imports: [Dialog, ReactiveFormsModule],
  templateUrl: './assignment-form-dialog.html',
})
export class AssignmentFormDialog {
  private readonly assignmentsService = inject(TeacherAssignmentsService);
  private readonly usersService = inject(UsersService);
  private readonly classesService = inject(ClassesService);
  private readonly subjectsService = inject(SubjectsService);

  readonly open = input(false);
  readonly defaults = input<{
    teacherId: number | null;
    classId: number | null;
    subjectId: number | null;
  }>({
    teacherId: null,
    classId: null,
    subjectId: null,
  });
  readonly saved = output<TeacherAssignment>();
  readonly closed = output<void>();

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  /** The three pick lists, loaded together when the dialog opens. */
  protected readonly options = rxResource({
    params: () => (this.open() ? true : undefined),
    stream: () =>
      forkJoin({
        teachers: this.usersService.getActiveStaff(),
        classes: this.classesService.getClasses({ pageSize: 100 }),
        subjects: this.subjectsService.getSubjects(),
      }),
  });

  protected readonly form = inject(NonNullableFormBuilder).group({
    teacherId: [null as number | null, [Validators.required]],
    classId: [null as number | null, [Validators.required]],
    subjectId: [null as number | null, [Validators.required]],
  });

  constructor() {
    effect(() => {
      if (this.open()) {
        this.errorMessage.set(null);
        this.form.reset(this.defaults());
      }
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
    const value = this.form.getRawValue();

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.assignmentsService
      .createAssignment({
        teacherId: value.teacherId!,
        classId: value.classId!,
        subjectId: value.subjectId!,
      })
      .subscribe({
        next: (assignment) => {
          this.submitting.set(false);
          this.saved.emit(assignment);
        },
        error: (error: unknown) => {
          this.submitting.set(false);
          this.errorMessage.set(toErrorMessage(error));
        },
      });
  }
}
