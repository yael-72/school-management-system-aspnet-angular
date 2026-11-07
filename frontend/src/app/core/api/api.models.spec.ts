import { toHttpParams } from './api.models';

describe('toHttpParams', () => {
  it('skips empty values', () => {
    const params = toHttpParams({ page: 2, search: '', classId: undefined, sortDir: 'desc' });

    expect(params.toString()).toBe('page=2&sortDir=desc');
  });
});
