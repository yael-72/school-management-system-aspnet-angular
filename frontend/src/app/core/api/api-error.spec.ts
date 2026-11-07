import { HttpErrorResponse } from '@angular/common/http';
import { toErrorMessage } from './api-error';

function httpError(status: number, error: unknown = null): HttpErrorResponse {
  return new HttpErrorResponse({ status, error });
}

describe('toErrorMessage', () => {
  it('translates a known business rule message to Hebrew', () => {
    const error = httpError(400, {
      detail:
        'Cannot delete a class that still has 3 active students. Move them to another class first.',
    });

    expect(toErrorMessage(error)).toContain('תלמידים פעילים');
  });

  it('uses a general message for validation errors', () => {
    const error = httpError(400, { errors: { Name: ['Name is required.'] } });

    expect(toErrorMessage(error)).toContain('השדות');
  });

  it('explains a 403 and a lost connection', () => {
    expect(toErrorMessage(httpError(403))).toContain('הרשאה');
    expect(toErrorMessage(httpError(0))).toContain('חיבור');
  });
});
