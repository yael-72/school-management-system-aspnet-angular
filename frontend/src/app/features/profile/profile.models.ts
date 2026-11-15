import { UserRole } from '../../core/auth/auth.models';

/** GET /api/users/me (ProfileDto). */
export interface Profile {
  id: number;
  fullName: string;
  email: string;
  phone: string | null;
  role: UserRole;
  /** ISO date-time. */
  createdAt: string;
  classCount: number;
  studentCount: number;
  assessmentCount: number;
}

/** Body of PUT /api/users/me. */
export interface UpdateProfileRequest {
  fullName: string;
  phone: string | null;
}

/** GET/PUT /api/users/me/notification-settings. */
export interface NotificationSettings {
  remindBeforeExam: boolean;
  alertOnLowAttendance: boolean;
  weeklySummaryEmail: boolean;
}

export interface UploadedFile {
  id: number;
  fileName: string;
  sizeBytes: number;
  uploadedAt: string;
}

/** GET /api/files */
export interface FilesOverview {
  files: UploadedFile[];
  usedBytes: number;
  quotaBytes: number;
}

/**
 * The same limits as FileRules in the API. Checking them here gives an instant message instead of
 * uploading 20 MB just to be told it is too big; the API still enforces them.
 */
export const FILE_RULES = {
  maxFileSizeBytes: 5 * 1024 * 1024,
  allowedExtensions: [
    '.pdf',
    '.doc',
    '.docx',
    '.xls',
    '.xlsx',
    '.ppt',
    '.pptx',
    '.txt',
    '.csv',
    '.png',
    '.jpg',
    '.jpeg',
    '.gif',
  ],
} as const;
