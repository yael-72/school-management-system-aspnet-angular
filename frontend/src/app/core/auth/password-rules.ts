import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * The password rule of the app (PasswordRules in the API): 8–100 characters with a letter and a digit.
 * Checked here for instant feedback; the API enforces it.
 */
export const PASSWORD_PATTERN = /^(?=.*[A-Za-z])(?=.*\d).{8,100}$/;

/** Form-group validator: the two fields must hold the same value ({ passwordMismatch: true } otherwise). */
export function matchingPasswords(field: string, confirmField: string): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const value = group.get(field)?.value;
    const confirm = group.get(confirmField)?.value;
    return value && confirm && value !== confirm ? { passwordMismatch: true } : null;
  };
}
