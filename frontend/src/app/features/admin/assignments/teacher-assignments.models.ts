import { ListQuery } from '../../../core/api/api.models';

/** One row of the assignments table (TeacherAssignmentDto). */
export interface TeacherAssignment {
  id: number;
  teacherId: number;
  teacherName: string;
  teacherIsActive: boolean;
  classId: number;
  className: string;
  gradeLevel: number;
  subjectId: number;
  subjectName: string;
}

/** Body of POST /api/teacher-assignments. */
export interface CreateTeacherAssignmentRequest {
  teacherId: number;
  classId: number;
  subjectId: number;
}

export type TeacherAssignmentSortField = 'className' | 'subjectName' | 'teacherName';

export interface TeacherAssignmentListQuery extends ListQuery {
  classId?: number;
  teacherId?: number;
  subjectId?: number;
  sortBy?: TeacherAssignmentSortField;
}
