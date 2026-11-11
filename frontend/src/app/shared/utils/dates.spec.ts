import { startOfMonthIsoDate, toIsoDate } from './dates';

describe('date helpers', () => {
  it('formats the local date with zero padding', () => {
    expect(toIsoDate(new Date(2026, 0, 5))).toBe('2026-01-05');
  });

  it('keeps the local day just after midnight (no UTC shift)', () => {
    expect(toIsoDate(new Date(2026, 10, 10, 0, 30))).toBe('2026-11-10');
  });

  it('returns the first day of the month', () => {
    expect(startOfMonthIsoDate(new Date(2026, 10, 23))).toBe('2026-11-01');
  });
});
