import { ListQuery } from '../../core/api/api.models';

/** One row of the students table (StudentListItemDto in the API). */
export interface StudentListItem {
  id: number;
  firstName: string;
  lastName: string;
  classId: number;
  className: string;
  /** ISO date "yyyy-MM-dd". */
  birthDate: string | null;
  parentPhone: string | null;
  /** Average score as a percentage (0–100), or null when nothing was graded yet. */
  averageScore: number | null;
}

/** StudentDto in the API. */
export interface Student {
  id: number;
  firstName: string;
  lastName: string;
  classId: number;
  className: string;
  birthDate: string | null;
  parentPhone: string | null;
  address: string | null;
}

export interface RecentScore {
  assessmentId: number;
  assessmentTitle: string;
  subjectName: string;
  date: string;
  score: number;
  maxScore: number;
  percent: number;
}

/** GET /api/students/{id}/summary */
export interface StudentSummary {
  student: Student;
  averageScore: number | null;
  /** Share of recorded days the student was present or late (0–100). */
  attendanceRate: number | null;
  absenceCount: number;
  lateCount: number;
  recentScores: RecentScore[];
}

/** Body of POST/PUT /api/students. */
export interface SaveStudentRequest {
  firstName: string;
  lastName: string;
  classId: number;
  birthDate: string | null;
  parentPhone: string | null;
  address: string | null;
}

export type StudentSortField = 'lastName' | 'className' | 'birthDate' | 'averageScore';

export interface StudentListQuery extends ListQuery {
  classId?: number;
  sortBy?: StudentSortField;
}
