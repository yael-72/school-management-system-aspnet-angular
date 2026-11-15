import { buildMonthGrid, isOnDay } from './month-grid';

describe('buildMonthGrid', () => {
  it('fills whole Sunday-to-Saturday weeks around the month', () => {
    // November 2026 starts on a Sunday and ends on a Monday.
    const days = buildMonthGrid(2026, 10);

    expect(days[0]).toEqual({ date: '2026-11-01', day: 1, inMonth: true });
    expect(days.at(-1)).toEqual({ date: '2026-12-05', day: 5, inMonth: false });
    expect(days.length % 7).toBe(0);
  });

  it('starts with days of the previous month when the 1st is not a Sunday', () => {
    // 1 December 2026 is a Tuesday.
    const days = buildMonthGrid(2026, 11);

    expect(days.slice(0, 3).map((d) => d.date)).toEqual(['2026-11-29', '2026-11-30', '2026-12-01']);
    expect(days[0].inMonth).toBe(false);
  });
});

describe('isOnDay', () => {
  it('includes every day of a multi-day event', () => {
    const trip = { startDate: '2026-11-24', endDate: '2026-11-25' };

    expect(isOnDay(trip, '2026-11-24')).toBe(true);
    expect(isOnDay(trip, '2026-11-25')).toBe(true);
    expect(isOnDay(trip, '2026-11-26')).toBe(false);
    expect(isOnDay({ startDate: '2026-11-24', endDate: null }, '2026-11-24')).toBe(true);
  });
});
