import { FormControl, FormGroup } from '@angular/forms';
import { PASSWORD_PATTERN, matchingPasswords } from './password-rules';

describe('password rules', () => {
  it('needs 8 characters with a letter and a digit', () => {
    expect(PASSWORD_PATTERN.test('Welcome123')).toBe(true);
    expect(PASSWORD_PATTERN.test('12345678')).toBe(false);
    expect(PASSWORD_PATTERN.test('abcdefgh')).toBe(false);
    expect(PASSWORD_PATTERN.test('Ab1')).toBe(false);
  });

  it('reports when the confirmation does not match', () => {
    const form = new FormGroup(
      { password: new FormControl('Welcome123'), confirm: new FormControl('Welcome124') },
      { validators: matchingPasswords('password', 'confirm') },
    );
    expect(form.hasError('passwordMismatch')).toBe(true);

    form.controls.confirm.setValue('Welcome123');
    expect(form.hasError('passwordMismatch')).toBe(false);
  });
});
