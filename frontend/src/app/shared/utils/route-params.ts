/**
 * Query params arrive as strings ("3"). Turns ?classId=3 into the number 3, and a missing
 * or invalid value ("", "abc", "-1") into null. Used as an input transform for route params.
 */
export function toPositiveId(value: string | undefined): number | null {
  const id = Number(value);
  return Number.isInteger(id) && id > 0 ? id : null;
}
