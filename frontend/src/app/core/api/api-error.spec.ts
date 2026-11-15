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

  it('does not confuse similar messages', () => {
    const error = httpError(400, {
      detail: 'Students 4, 9 are not active students of this class.',
    });

    expect(toErrorMessage(error)).toContain('אינם רשומים בכיתה');
  });

  it('translates the score limit message', () => {
    const error = httpError(400, {
      detail: 'Score of student 4 must be between 0 and the max score (20).',
    });

    expect(toErrorMessage(error)).toContain('לציון המרבי');
  });

  it('translates an upload over the size limit', () => {
    const error = httpError(400, {
      errors: {
        '': ['Failed to read the request form. Multipart body length limit 6291456 exceeded.'],
      },
    });

    expect(toErrorMessage(error)).toContain('גדול מדי');
    expect(toErrorMessage(httpError(413))).toContain('גדול מדי');
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
