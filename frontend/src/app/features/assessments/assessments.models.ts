import { ListQuery } from '../../core/api/api.models';

/** Matches the AssessmentType enum in the API (sent as its name). */
export type AssessmentType = 'Exam' | 'Quiz' | 'Assignment';

/** The types in the order they are shown, with their Hebrew labels. */
export const ASSESSMENT_TYPES: readonly { value: AssessmentType; label: string }[] = [
  { value: 'Exam', label: 'מבחן' },
  { value: 'Quiz', label: 'בוחן' },
  { value: 'Assignment', label: 'עבודה' },
];

export function assessmentTypeLabel(type: AssessmentType): string {
  return ASSESSMENT_TYPES.find((t) => t.value === type)?.label ?? type;
}

/** One row of the assessments table (AssessmentListItemDto). */
export interface AssessmentListItem {
  id: number;
  title: string;
  type: AssessmentType;
  classId: number;
  className: string;
  subjectId: number;
  subjectName: string;
  /** ISO date "yyyy-MM-dd". */
  date: string;
  maxScore: number;
  /** Active students who already have a score. */
  gradedCount: number;
  /** Active students in the class. */
  studentCount: number;
  /** Average as a percentage (0–100), or null when nothing was graded yet. */
  averageScore: number | null;
  /** Whether the current user may edit the assessment and enter its scores. */
  canEdit: boolean;
}

/** GET /api/assessments/{id} (AssessmentDto). */
export interface Assessment {
  id: number;
  title: string;
  type: AssessmentType;
  classId: number;
  className: string;
  subjectId: number;
  subjectName: string;
  date: string;
  maxScore: number;
  createdByName: string;
  canEdit: boolean;
}

/** Body of POST/PUT /api/assessments. */
export interface SaveAssessmentRequest {
  title: string;
  type: AssessmentType;
  classId: number;
  subjectId: number;
  date: string;
  maxScore: number;
}

/** One row of the grade sheet. `score` is null when the student was not graded yet. */
export interface StudentScore {
  studentId: number;
  firstName: string;
  lastName: string;
  score: number | null;
  /** score / maxScore as a percentage (0–100). */
  percent: number | null;
  comment: string | null;
}

/** GET /api/assessments/{id}/scores – the grade sheet. */
export interface AssessmentScores {
  assessment: Assessment;
  gradedCount: number;
  /** Average as a percentage (0–100). */
  averageScore: number | null;
  /** Raw scores (out of maxScore), not percentages. */
  highestScore: number | null;
  lowestScore: number | null;
  students: StudentScore[];
}

/** Body of PUT /api/assessments/{id}/scores. A null score with no comment clears the score. */
export interface SaveScoresRequest {
  scores: { studentId: number; score: number | null; comment: string | null }[];
}

export type AssessmentSortField = 'date' | 'title' | 'className' | 'subjectName' | 'averageScore';

export interface AssessmentListQuery extends ListQuery {
  classId?: number;
  subjectId?: number;
  type?: AssessmentType;
  from?: string;
  to?: string;
  sortBy?: AssessmentSortField;
}
