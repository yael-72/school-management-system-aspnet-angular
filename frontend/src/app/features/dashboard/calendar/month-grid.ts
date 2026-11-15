import { toIsoDate } from '../../../shared/utils/dates';

export interface CalendarDay {
  /** ISO date "yyyy-MM-dd". */
  date: string;
  day: number;
  /** False for the days of the previous / next month that fill the first and last week. */
  inMonth: boolean;
}

/**
 * The days shown for a month: whole weeks from Sunday to Saturday (the Israeli week), starting
 * with the week of the 1st and ending with the week of the last day. `month` is 0–11, like Date.
 */
export function buildMonthGrid(year: number, month: number): CalendarDay[] {
  const first = new Date(year, month, 1);
  const last = new Date(year, month + 1, 0);
  const start = new Date(year, month, 1 - first.getDay());
  const end = new Date(year, month, last.getDate() + (6 - last.getDay()));

  const days: CalendarDay[] = [];
  for (
    let date = start;
    date <= end;
    date = new Date(date.getFullYear(), date.getMonth(), date.getDate() + 1)
  ) {
    days.push({ date: toIsoDate(date), day: date.getDate(), inMonth: date.getMonth() === month });
  }
  return days;
}

/** Whether an event (startDate..endDate, both included) takes place on the given day. */
export function isOnDay(
  event: { startDate: string; endDate: string | null },
  date: string,
): boolean {
  return event.startDate <= date && (event.endDate ?? event.startDate) >= date;
}
