/** A student with many recent absences (FrequentAbsenceDto). */
export interface FrequentAbsence {
  studentId: number;
  firstName: string;
  lastName: string;
  className: string;
  absenceCount: number;
}

/** GET /api/dashboard/stats */
export interface DashboardStats {
  studentCount: number;
  classCount: number;
  /** 0–100, or null when no attendance was recorded today. */
  todayAttendanceRate: number | null;
  /** 0–100, or null when nothing was graded. */
  averageScore: number | null;
  examsThisWeek: number;
  frequentAbsences: FrequentAbsence[];
}

/** One point of GET /api/dashboard/grades-trend. */
export interface MonthlyAverage {
  year: number;
  /** 1–12 */
  month: number;
  averageScore: number | null;
}

/** One bar of GET /api/dashboard/attendance-weekly. */
export interface DailyAttendance {
  /** ISO date "yyyy-MM-dd". */
  date: string;
  present: number;
  late: number;
  absent: number;
  excused: number;
  attendanceRate: number | null;
}

/** GET /api/dashboard/grade-distribution – number of scores in each grade band. */
export interface GradeDistribution {
  excellent: number;
  good: number;
  fair: number;
  low: number;
}

export interface TopStudent {
  studentId: number;
  firstName: string;
  lastName: string;
  className: string;
  averageScore: number;
}

export type ActivityKind = 'AssessmentCreated' | 'ScoresEntered';

export interface RecentActivity {
  kind: ActivityKind;
  assessmentId: number;
  title: string;
  className: string;
  subjectName: string;
  /** ISO date-time (UTC). */
  occurredAt: string;
  userName: string | null;
}
