import { Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { toErrorMessage } from '../../../core/api/api-error';
import { AuthService } from '../../../core/auth/auth.service';
import { PASSWORD_PATTERN } from '../../../core/auth/password-rules';
import { ROLE_LABELS } from '../../../core/auth/role-labels';
import { UserDetails } from '../../../core/users/users.models';
import { UsersService } from '../../../core/users/users.service';
import { Dialog } from '../../../shared/components/dialog/dialog';

/** Same rules as the API. */
const PHONE_PATTERN = /^[0-9+\-\s]{7,20}$/;

/**
 * Add a staff member (with a first password) or edit one (pass `userId`). The password field exists
 * only when adding; an admin cannot change her own role.
 */
@Component({
  selector: 'app-user-form-dialog',
  imports: [Dialog, ReactiveFormsModule],
  templateUrl: './user-form-dialog.html',
})
export class UserFormDialog {
  private readonly usersService = inject(UsersService);
  private readonly authService = inject(AuthService);

  readonly open = input(false);
  readonly userId = input<number | null>(null);
  readonly saved = output<UserDetails>();
  readonly closed = output<void>();

  protected readonly roleLabels = ROLE_LABELS;
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly showPassword = signal(false);

  protected readonly isNew = computed(() => this.userId() === null);
  protected readonly isSelf = computed(() => this.userId() === this.authService.currentUser()?.id);

  protected readonly roles = rxResource({
    params: () => (this.open() ? true : undefined),
    stream: () => this.usersService.getRoles(),
  });

  private readonly user = rxResource({
    params: () => (this.open() ? (this.userId() ?? undefined) : undefined),
    stream: ({ params: id }) => this.usersService.getUser(id),
  });

  protected readonly loadingUser = this.user.isLoading;

  protected readonly form = inject(NonNullableFormBuilder).group({
    fullName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(150)]],
    phone: ['', [Validators.pattern(PHONE_PATTERN)]],
    roleId: [null as number | null, [Validators.required]],
    password: ['', [Validators.required, Validators.pattern(PASSWORD_PATTERN)]],
  });

  constructor() {
    // Reset the form when the dialog opens, and fill it once the edited user has loaded.
    effect(() => {
      if (!this.open()) {
        return;
      }
      const user = this.user.value();
      this.errorMessage.set(null);
      this.showPassword.set(false);
      this.form.reset({
        fullName: user?.fullName ?? '',
        email: user?.email ?? '',
        phone: user?.phone ?? '',
        roleId: user?.roleId ?? null,
        password: '',
      });

      // The password is set only when adding a user; the role of yourself stays as it is.
      const { password, roleId } = this.form.controls;
      if (this.isNew()) {
        password.enable();
      } else {
        password.disable();
      }
      if (this.isSelf()) {
        roleId.disable();
      } else {
        roleId.enable();
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
    const details = {
      fullName: value.fullName.trim(),
      email: value.email.trim(),
      phone: value.phone.trim() || null,
      roleId: value.roleId!,
    };

    const id = this.userId();
    const save$ = id
      ? this.usersService.updateUser(id, details)
      : this.usersService.createUser({ ...details, password: value.password });

    this.submitting.set(true);
    this.errorMessage.set(null);
    save$.subscribe({
      next: (user) => {
        this.submitting.set(false);
        this.saved.emit(user);
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.errorMessage.set(toErrorMessage(error));
      },
    });
  }
}
