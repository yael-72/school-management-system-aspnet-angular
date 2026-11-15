import { DatePipe } from '@angular/common';
import { Component, inject, input, output, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { toErrorMessage } from '../../core/api/api-error';
import { Profile } from './profile.models';
import { ProfileService } from './profile.service';
import { ROLE_LABELS } from './role-labels';

/** Same rule as the API: digits, spaces, "+" and "-", 7–20 characters. */
const PHONE_PATTERN = /^[0-9+\-\s]{7,20}$/;

/**
 * The user's details as tiles (Horizon "General Information"), with an edit mode for the fields
 * she may change: name and phone. Email (the login name) and role are set by the admin.
 */
@Component({
  selector: 'app-personal-details',
  imports: [DatePipe, ReactiveFormsModule],
  templateUrl: './personal-details.html',
  styleUrl: './personal-details.scss',
})
export class PersonalDetails {
  private readonly profileService = inject(ProfileService);

  readonly profile = input.required<Profile>();
  readonly saved = output<Profile>();

  protected readonly roleLabels = ROLE_LABELS;
  protected readonly editing = signal(false);
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = inject(NonNullableFormBuilder).group({
    fullName: ['', [Validators.required, Validators.maxLength(100)]],
    phone: ['', [Validators.pattern(PHONE_PATTERN)]],
  });

  protected startEditing(): void {
    const profile = this.profile();
    this.form.reset({ fullName: profile.fullName, phone: profile.phone ?? '' });
    this.errorMessage.set(null);
    this.editing.set(true);
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
    this.profileService
      .updateProfile({ fullName: value.fullName.trim(), phone: value.phone.trim() || null })
      .subscribe({
        next: (profile) => {
          this.submitting.set(false);
          this.editing.set(false);
          this.saved.emit(profile);
        },
        error: (error: unknown) => {
          this.submitting.set(false);
          this.errorMessage.set(toErrorMessage(error));
        },
      });
  }
}
