import { toPositiveId } from './route-params';

describe('toPositiveId', () => {
  it('parses a positive integer', () => {
    expect(toPositiveId('3')).toBe(3);
  });

  it.each([undefined, '', 'abc', '0', '-1', '2.5'])('returns null for %s', (value) => {
    expect(toPositiveId(value)).toBeNull();
  });
});
