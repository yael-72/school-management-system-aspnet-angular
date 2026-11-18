import { Component, effect, inject, input, output, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { toErrorMessage } from '../../core/api/api-error';
import { PASSWORD_PATTERN, matchingPasswords } from '../../core/auth/password-rules';
import { Dialog } from '../../shared/components/dialog/dialog';
import { ProfileService } from './profile.service';

/** Change your own password: the current one, then the new one twice. */
@Component({
  selector: 'app-change-password-dialog',
  imports: [Dialog, ReactiveFormsModule],
  templateUrl: './change-password-dialog.html',
})
export class ChangePasswordDialog {
  private readonly profileService = inject(ProfileService);

  readonly open = input(false);
  readonly changed = output<void>();
  readonly closed = output<void>();

  protected readonly showPasswords = signal(false);
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = inject(NonNullableFormBuilder).group(
    {
      currentPassword: ['', [Validators.required]],
      newPassword: ['', [Validators.required, Validators.pattern(PASSWORD_PATTERN)]],
      confirmPassword: ['', [Validators.required]],
    },
    { validators: matchingPasswords('newPassword', 'confirmPassword') },
  );

  constructor() {
    // Never keep passwords in the form after the dialog closes.
    effect(() => {
      if (this.open()) {
        this.form.reset();
        this.errorMessage.set(null);
        this.showPasswords.set(false);
      }
    });
  }

  protected isInvalid(control: keyof typeof this.form.controls): boolean {
    const field = this.form.controls[control];
    return field.invalid && field.touched;
  }

  protected readonly mismatch = () =>
    this.form.hasError('passwordMismatch') && this.form.controls.confirmPassword.touched;

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const { currentPassword, newPassword } = this.form.getRawValue();

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.profileService.changePassword({ currentPassword, newPassword }).subscribe({
      next: () => {
        this.submitting.set(false);
        this.changed.emit();
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.errorMessage.set(toErrorMessage(error));
      },
    });
  }
}
