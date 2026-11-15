const UNITS = ['B', 'KB', 'MB', 'GB'];

/** 1536 → "1.5 KB". Used for file sizes and storage usage. */
export function formatBytes(bytes: number): string {
  let value = bytes;
  let unit = 0;
  while (value >= 1024 && unit < UNITS.length - 1) {
    value /= 1024;
    unit++;
  }
  const rounded = unit === 0 ? value : Math.round(value * 10) / 10;
  return `${rounded} ${UNITS[unit]}`;
}
