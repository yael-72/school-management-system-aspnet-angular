/**
 * The API exchanges dates as ISO strings without a time ("2026-11-10").
 * Built from the local date parts on purpose: `toISOString()` converts to UTC first,
 * which in Israel would return yesterday's date between midnight and 02:00/03:00.
 */
export function toIsoDate(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

/** Today's local date as "yyyy-MM-dd". */
export function todayIsoDate(): string {
  return toIsoDate(new Date());
}

/** The first day of the month of the given local date, as "yyyy-MM-dd". */
export function startOfMonthIsoDate(date: Date = new Date()): string {
  return toIsoDate(new Date(date.getFullYear(), date.getMonth(), 1));
}
