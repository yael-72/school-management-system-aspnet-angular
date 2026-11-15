/** Matches the AttendanceStatus enum in the API (sent as its name). */
export type AttendanceStatus = 'Present' | 'Absent' | 'Late' | 'Excused';

/** The statuses in the order they are shown, with their Hebrew labels. */
export const ATTENDANCE_STATUSES: readonly { value: AttendanceStatus; label: string }[] = [
  { value: 'Present', label: 'נוכח' },
  { value: 'Absent', label: 'חסר' },
  { value: 'Late', label: 'איחור' },
  { value: 'Excused', label: 'חיסור מוצדק' },
];

/** One row of the register (AttendanceEntryDto). `status` is null when nothing was recorded yet. */
export interface AttendanceEntry {
  studentId: number;
  firstName: string;
  lastName: string;
  status: AttendanceStatus | null;
  note: string | null;
}

/** GET /api/attendance?classId=&date= – a class's register on one day. */
export interface ClassAttendance {
  classId: number;
  className: string;
  /** ISO date "yyyy-MM-dd". */
  date: string;
  students: AttendanceEntry[];
}

/** Body of PUT /api/attendance. */
export interface SaveAttendanceRequest {
  classId: number;
  date: string;
  records: { studentId: number; status: AttendanceStatus; note: string | null }[];
}

export interface StudentAttendanceSummary {
  studentId: number;
  firstName: string;
  lastName: string;
  present: number;
  absent: number;
  late: number;
  excused: number;
  /** (present + late) / recorded days, 0–100; null when no day was recorded. */
  attendanceRate: number | null;
}

/** GET /api/attendance/report?classId=&from=&to= */
export interface AttendanceReport {
  classId: number;
  className: string;
  from: string;
  to: string;
  attendanceRate: number | null;
  students: StudentAttendanceSummary[];
}
