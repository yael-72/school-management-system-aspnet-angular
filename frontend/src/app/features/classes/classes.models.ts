import { ListQuery } from '../../core/api/api.models';

/** Matches ClassSummaryDto in the API. */
export interface ClassSummary {
  id: number;
  name: string;
  gradeLevel: number;
  schoolYear: string;
  homeroomTeacherId: number | null;
  homeroomTeacherName: string | null;
  studentCount: number;
  /** Average score of the class as a percentage (0–100), or null when nothing was graded yet. */
  averageScore: number | null;
}

export interface ClassSubject {
  subjectId: number;
  subjectName: string;
  teacherId: number;
  teacherName: string;
}

/** Matches ClassDetailsDto in the API. */
export interface ClassDetails extends ClassSummary {
  subjects: ClassSubject[];
}

/** Body of POST/PUT /api/classes. */
export interface SaveClassRequest {
  name: string;
  gradeLevel: number;
  schoolYear: string;
  homeroomTeacherId: number | null;
}

export interface ClassListQuery extends ListQuery {
  gradeLevel?: number;
}

/** Hebrew grade names, as used in Israeli schools: 7 → ז', 12 → י"ב. */
export const GRADE_LEVEL_NAMES: Readonly<Record<number, string>> = {
  1: "א'",
  2: "ב'",
  3: "ג'",
  4: "ד'",
  5: "ה'",
  6: "ו'",
  7: "ז'",
  8: "ח'",
  9: "ט'",
  10: "י'",
  11: 'י"א',
  12: 'י"ב',
};
