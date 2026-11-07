import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { ThemeService } from '../../../core/theme/theme.service';

/** Demo account shown on the login page so visitors can try the app (seeded by the API in development). */
const DEMO_ACCOUNT = { email: 'admin@school.demo', password: 'Demo123!' };

@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule],
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss',
})
export class LoginPage {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  protected readonly themeService = inject(ThemeService);

  /** Filled from the ?returnUrl= query parameter (withComponentInputBinding). */
  readonly returnUrl = input<string>();

  protected readonly demoAccount = DEMO_ACCOUNT;
  protected readonly currentYear = new Date().getFullYear();
  protected readonly showPassword = signal(false);
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = inject(NonNullableFormBuilder).group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
    rememberMe: [false],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    this.authService.login(this.form.getRawValue()).subscribe({
      next: () => void this.router.navigateByUrl(this.safeReturnUrl()),
      error: (error: unknown) => {
        this.submitting.set(false);
        this.errorMessage.set(toErrorMessage(error));
      },
    });
  }

  protected fillDemoAccount(): void {
    this.form.patchValue(this.demoAccount);
  }

  protected isInvalid(control: 'email' | 'password'): boolean {
    const field = this.form.controls[control];
    return field.invalid && field.touched;
  }

  /** Only internal paths, so a crafted link cannot send the user to another site after login. */
  private safeReturnUrl(): string {
    const url = this.returnUrl();
    return url && url.startsWith('/') && !url.startsWith('//') ? url : '/dashboard';
  }
}

function toErrorMessage(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'אירעה שגיאה לא צפויה. נסו שוב.';
  }
  if (error.status === 0) {
    return 'אין חיבור לשרת. בדקו את החיבור ונסו שוב.';
  }
  if (error.status === 401) {
    const detail = (error.error as { detail?: string } | null)?.detail;
    return detail === 'This account is disabled.'
      ? 'החשבון שלך מושבת. פנו להנהלת בית הספר.'
      : 'אימייל או סיסמה שגויים.';
  }
  return 'אירעה שגיאה בשרת. נסו שוב מאוחר יותר.';
}
