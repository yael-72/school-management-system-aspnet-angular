import { Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { toErrorMessage } from '../../../core/api/api-error';
import { AuthService } from '../../../core/auth/auth.service';
import { Dialog } from '../../../shared/components/dialog/dialog';
import { ClassesService } from '../../classes/classes.service';
import { SchoolEvent } from './events.models';
import { EventsService } from './events.service';

/**
 * Adds a calendar event. Admins may add school-wide events (no class); a teacher must pick one of
 * her classes – the API enforces the same rule.
 */
@Component({
  selector: 'app-event-form-dialog',
  imports: [Dialog, ReactiveFormsModule],
  templateUrl: './event-form-dialog.html',
})
export class EventFormDialog {
  private readonly eventsService = inject(EventsService);
  private readonly classesService = inject(ClassesService);
  private readonly authService = inject(AuthService);

  readonly open = input(false);
  /** The day the new event starts on (the day selected in the calendar). */
  readonly date = input.required<string>();
  readonly saved = output<SchoolEvent>();
  readonly closed = output<void>();

  protected readonly isAdmin = computed(() => this.authService.hasRole('Admin'));
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly classes = rxResource({
    params: () => (this.open() ? true : undefined),
    stream: () => this.classesService.getClasses({ pageSize: 100 }),
  });

  protected readonly form = inject(NonNullableFormBuilder).group({
    title: ['', [Validators.required, Validators.maxLength(100)]],
    startDate: ['', [Validators.required]],
    endDate: [''],
    classId: [null as number | null],
  });

  constructor() {
    effect(() => {
      if (!this.open()) {
        return;
      }
      this.errorMessage.set(null);
      this.form.reset({ title: '', startDate: this.date(), endDate: '', classId: null });
      // School-wide events are for admins only, so a teacher must choose a class.
      this.form.controls.classId.setValidators(this.isAdmin() ? [] : [Validators.required]);
      this.form.controls.classId.updateValueAndValidity();
    });
  }

  protected isInvalid(control: keyof typeof this.form.controls): boolean {
    const field = this.form.controls[control];
    return field.invalid && field.touched;
  }

  protected submit(): void {
    const value = this.form.getRawValue();
    if (this.form.invalid || (value.endDate && value.endDate < value.startDate)) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.eventsService
      .createEvent({
        title: value.title.trim(),
        startDate: value.startDate,
        endDate: value.endDate || null,
        classId: value.classId,
      })
      .subscribe({
        next: (event) => {
          this.submitting.set(false);
          this.saved.emit(event);
        },
        error: (error: unknown) => {
          this.submitting.set(false);
          this.errorMessage.set(toErrorMessage(error));
        },
      });
  }
}
