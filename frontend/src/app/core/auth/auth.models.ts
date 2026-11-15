/** Matches the role names in the API (SchoolManagement.Domain.Constants.RoleNames). */
export type UserRole = 'Admin' | 'Teacher';

export interface CurrentUser {
  id: number;
  fullName: string;
  email: string;
  role: UserRole;
}

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe: boolean;
}

export interface LoginResponse {
  accessToken: string;
  /** ISO date string (UTC). */
  expiresAt: string;
  user: CurrentUser;
}
