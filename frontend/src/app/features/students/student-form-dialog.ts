import { Component, effect, inject, input, output, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { toErrorMessage } from '../../core/api/api-error';
import { Dialog } from '../../shared/components/dialog/dialog';
import { ClassesService } from '../classes/classes.service';
import { SaveStudentRequest, Student } from './students.models';
import { StudentsService } from './students.service';

/** Same rule as the API: digits, spaces, "+" and "-", 7–20 characters. */
const PHONE_PATTERN = /^[0-9+\-\s]{7,20}$/;

/**
 * Enrol a new student or edit an existing one (admin only).
 * Pass `studentId` to edit (the full student is loaded from the API), or null to create.
 */
@Component({
  selector: 'app-student-form-dialog',
  imports: [Dialog, ReactiveFormsModule],
  templateUrl: './student-form-dialog.html',
})
export class StudentFormDialog {
  private readonly studentsService = inject(StudentsService);
  private readonly classesService = inject(ClassesService);

  readonly open = input(false);
  readonly studentId = input<number | null>(null);
  /** Pre-selected class for a new student (for example the class the list is filtered by). */
  readonly defaultClassId = input<number | null>(null);
  readonly saved = output<Student>();
  readonly closed = output<void>();

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly today = new Date().toISOString().slice(0, 10);

  protected readonly classes = rxResource({
    params: () => (this.open() ? true : undefined),
    stream: () => this.classesService.getClasses({ pageSize: 100 }),
  });

  /** The student being edited, loaded when the dialog opens in edit mode. */
  private readonly student = rxResource({
    params: () => (this.open() ? (this.studentId() ?? undefined) : undefined),
    stream: ({ params: id }) => this.studentsService.getStudent(id),
  });

  protected readonly form = inject(NonNullableFormBuilder).group({
    firstName: ['', [Validators.required, Validators.maxLength(50)]],
    lastName: ['', [Validators.required, Validators.maxLength(50)]],
    classId: [null as number | null, [Validators.required]],
    birthDate: [''],
    parentPhone: ['', [Validators.pattern(PHONE_PATTERN)]],
    address: ['', [Validators.maxLength(200)]],
  });

  constructor() {
    // Reset the form when the dialog opens, and fill it once the edited student has loaded.
    effect(() => {
      if (!this.open()) {
        return;
      }
      const student = this.student.value();
      this.errorMessage.set(null);
      this.form.reset({
        firstName: student?.firstName ?? '',
        lastName: student?.lastName ?? '',
        classId: student?.classId ?? this.defaultClassId(),
        birthDate: student?.birthDate ?? '',
        parentPhone: student?.parentPhone ?? '',
        address: student?.address ?? '',
      });
    });
  }

  protected readonly loadingStudent = this.student.isLoading;

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
    const request: SaveStudentRequest = {
      firstName: value.firstName.trim(),
      lastName: value.lastName.trim(),
      classId: value.classId!,
      // Empty optional fields are sent as null, not as "".
      birthDate: value.birthDate || null,
      parentPhone: value.parentPhone.trim() || null,
      address: value.address.trim() || null,
    };

    const id = this.studentId();
    const save$ = id
      ? this.studentsService.updateStudent(id, request)
      : this.studentsService.createStudent(request);

    this.submitting.set(true);
    this.errorMessage.set(null);
    save$.subscribe({
      next: (student) => {
        this.submitting.set(false);
        this.saved.emit(student);
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.errorMessage.set(toErrorMessage(error));
      },
    });
  }
}
