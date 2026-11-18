import { ListQuery } from '../api/api.models';
import { UserRole } from '../auth/auth.models';

/** One row of the staff table (UserListItemDto). */
export interface UserListItem {
  id: number;
  fullName: string;
  email: string;
  role: UserRole;
  phone: string | null;
  isActive: boolean;
  /** How many (class, subject) pairs the user teaches. */
  assignmentCount: number;
}

/** GET /api/users/{id} (UserDto). */
export interface UserDetails {
  id: number;
  fullName: string;
  email: string;
  phone: string | null;
  roleId: number;
  role: UserRole;
  isActive: boolean;
  createdAt: string;
}

export interface Role {
  id: number;
  name: UserRole;
}

/** Body of PUT /api/users/{id}. */
export interface UpdateUserRequest {
  fullName: string;
  email: string;
  phone: string | null;
  roleId: number;
}

/** Body of POST /api/users: the admin chooses a first password and gives it to the new user. */
export interface CreateUserRequest extends UpdateUserRequest {
  password: string;
}

export type UserSortField = 'fullName' | 'email' | 'role' | 'assignmentCount';

export interface UserListQuery extends ListQuery {
  role?: UserRole;
  isActive?: boolean;
  sortBy?: UserSortField;
}
