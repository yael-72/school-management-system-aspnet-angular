import { Component, effect, inject, input, output, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { toErrorMessage } from '../../../core/api/api-error';
import { PASSWORD_PATTERN } from '../../../core/auth/password-rules';
import { UserListItem } from '../../../core/users/users.models';
import { UsersService } from '../../../core/users/users.service';
import { Dialog } from '../../../shared/components/dialog/dialog';

/**
 * The admin sets a new password for a user who forgot hers, and gives it to her.
 * The password is shown as plain text, because the admin has to pass it on.
 */
@Component({
  selector: 'app-reset-password-dialog',
  imports: [Dialog, ReactiveFormsModule],
  template: `
    <app-dialog [open]="user() !== null" title="איפוס סיסמה" (closed)="closed.emit()">
      <form class="form-grid" [formGroup]="form" (ngSubmit)="submit()" novalidate>
        @if (user(); as target) {
          <p>
            סיסמה חדשה עבור <strong>{{ target.fullName }}</strong
            >. מסרו אותה למשתמש.
          </p>
        }
        @if (errorMessage(); as message) {
          <p class="form-alert" role="alert">{{ message }}</p>
        }
        <div class="form-field">
          <label class="form-label" for="reset-password"
            >סיסמה חדשה<span class="form-label__required">*</span></label
          >
          <input
            id="reset-password"
            class="form-input"
            dir="ltr"
            autocomplete="off"
            formControlName="newPassword"
            [class.is-invalid]="isInvalid()"
          />
          @if (isInvalid()) {
            <span class="form-error">8 תווים לפחות, ובהם אות וספרה.</span>
          } @else {
            <span class="form-hint">8 תווים לפחות, ובהם אות וספרה.</span>
          }
        </div>
        <div class="form-actions">
          <button type="submit" class="button" [disabled]="submitting()">
            {{ submitting() ? 'שומר...' : 'איפוס' }}
          </button>
          <button type="button" class="button button--secondary" (click)="closed.emit()">
            ביטול
          </button>
        </div>
      </form>
    </app-dialog>
  `,
})
export class ResetPasswordDialog {
  private readonly usersService = inject(UsersService);

  /** The user whose password is reset; the dialog is open while this is set. */
  readonly user = input<UserListItem | null>(null);
  readonly done = output<void>();
  readonly closed = output<void>();

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  // A form group (not a lone control) so that (ngSubmit) works: it comes from [formGroup].
  protected readonly form = inject(NonNullableFormBuilder).group({
    newPassword: ['', [Validators.required, Validators.pattern(PASSWORD_PATTERN)]],
  });

  constructor() {
    effect(() => {
      if (this.user()) {
        this.form.reset();
        this.errorMessage.set(null);
      }
    });
  }

  protected isInvalid(): boolean {
    const field = this.form.controls.newPassword;
    return field.invalid && field.touched;
  }

  protected submit(): void {
    const user = this.user();
    if (!user || this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.usersService.resetPassword(user.id, this.form.controls.newPassword.value).subscribe({
      next: () => {
        this.submitting.set(false);
        this.done.emit();
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.errorMessage.set(toErrorMessage(error));
      },
    });
  }
}
