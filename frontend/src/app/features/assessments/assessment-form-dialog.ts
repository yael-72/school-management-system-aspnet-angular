import { Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { toErrorMessage } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { Subject, SubjectsService } from '../../core/subjects/subjects.service';
import { Dialog } from '../../shared/components/dialog/dialog';
import { todayIsoDate } from '../../shared/utils/dates';
import { ClassesService } from '../classes/classes.service';
import { AssessmentsService } from './assessments.service';
import {
  ASSESSMENT_TYPES,
  Assessment,
  AssessmentType,
  SaveAssessmentRequest,
} from './assessments.models';

/** The fields the dialog needs from an existing assessment (a list row or the full assessment). */
export type EditableAssessment = Pick<
  Assessment,
  'id' | 'title' | 'type' | 'classId' | 'subjectId' | 'date' | 'maxScore'
>;

/**
 * Create an assessment, or edit one (pass `assessment`).
 *
 * The subject list shows only what the user may grade: every subject for an admin, and for a
 * teacher the subjects she teaches in the chosen class. The API enforces the same rule (403);
 * filtering here just keeps the user from choosing something that would be rejected.
 */
@Component({
  selector: 'app-assessment-form-dialog',
  imports: [Dialog, ReactiveFormsModule],
  templateUrl: './assessment-form-dialog.html',
})
export class AssessmentFormDialog {
  private readonly assessmentsService = inject(AssessmentsService);
  private readonly classesService = inject(ClassesService);
  private readonly subjectsService = inject(SubjectsService);
  private readonly authService = inject(AuthService);

  readonly open = input(false);
  readonly assessment = input<EditableAssessment | null>(null);
  /** Pre-selected class for a new assessment (for example the class the list is filtered by). */
  readonly defaultClassId = input<number | null>(null);
  readonly saved = output<Assessment>();
  readonly closed = output<void>();

  protected readonly types = ASSESSMENT_TYPES;
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = inject(NonNullableFormBuilder).group({
    title: ['', [Validators.required, Validators.maxLength(100)]],
    type: ['Exam' as AssessmentType, [Validators.required]],
    classId: [null as number | null, [Validators.required]],
    subjectId: [null as number | null, [Validators.required]],
    date: [todayIsoDate(), [Validators.required]],
    maxScore: [100, [Validators.required, Validators.min(1), Validators.max(999)]],
  });

  private readonly isAdmin = computed(() => this.authService.hasRole('Admin'));
  private readonly selectedClassId = toSignal(this.form.controls.classId.valueChanges, {
    initialValue: null,
  });

  protected readonly classes = rxResource({
    params: () => (this.open() ? true : undefined),
    stream: () => this.classesService.getClasses({ pageSize: 100 }),
  });

  /** Admins can choose any subject. */
  private readonly allSubjects = rxResource({
    params: () => (this.open() && this.isAdmin() ? true : undefined),
    stream: () => this.subjectsService.getSubjects(),
  });

  /** Teachers: the chosen class's details tell who teaches which subject in it. */
  private readonly classDetails = rxResource({
    params: () =>
      this.open() && !this.isAdmin() ? (this.selectedClassId() ?? undefined) : undefined,
    stream: ({ params: classId }) => this.classesService.getClass(classId),
  });

  protected readonly subjectOptions = computed<Subject[]>(() => {
    if (this.isAdmin()) {
      return this.allSubjects.value() ?? [];
    }
    const userId = this.authService.currentUser()?.id;
    return (this.classDetails.value()?.subjects ?? [])
      .filter((s) => s.teacherId === userId)
      .map((s) => ({ id: s.subjectId, name: s.subjectName }));
  });

  protected readonly loadingSubjects = computed(
    () => this.allSubjects.isLoading() || this.classDetails.isLoading(),
  );

  /** A teacher picked a class where she teaches nothing (for example her homeroom class only). */
  protected readonly noSubjectsInClass = computed(
    () =>
      !this.isAdmin() &&
      this.selectedClassId() !== null &&
      this.classDetails.hasValue() &&
      this.subjectOptions().length === 0,
  );

  constructor() {
    // Reset the form whenever the dialog opens.
    effect(() => {
      if (!this.open()) {
        return;
      }
      const assessment = this.assessment();
      this.errorMessage.set(null);
      this.form.reset({
        title: assessment?.title ?? '',
        type: assessment?.type ?? 'Exam',
        classId: assessment?.classId ?? this.defaultClassId(),
        subjectId: assessment?.subjectId ?? null,
        date: assessment?.date ?? todayIsoDate(),
        maxScore: assessment?.maxScore ?? 100,
      });
    });

    // When the subject options change (another class was chosen): clear a subject the user cannot
    // choose there, and pick the only option automatically – a teacher usually teaches one subject.
    effect(() => {
      const options = this.subjectOptions();
      const subjectControl = this.form.controls.subjectId;
      if (this.loadingSubjects()) {
        return;
      }
      if (subjectControl.value !== null && !options.some((s) => s.id === subjectControl.value)) {
        subjectControl.setValue(null);
      }
      if (subjectControl.value === null && options.length === 1) {
        subjectControl.setValue(options[0].id);
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
    const request: SaveAssessmentRequest = {
      title: value.title.trim(),
      type: value.type,
      classId: value.classId!,
      subjectId: value.subjectId!,
      date: value.date,
      maxScore: value.maxScore,
    };

    const id = this.assessment()?.id;
    const save$ = id
      ? this.assessmentsService.updateAssessment(id, request)
      : this.assessmentsService.createAssessment(request);

    this.submitting.set(true);
    this.errorMessage.set(null);
    save$.subscribe({
      next: (assessment) => {
        this.submitting.set(false);
        this.saved.emit(assessment);
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.errorMessage.set(toErrorMessage(error));
      },
    });
  }
}
