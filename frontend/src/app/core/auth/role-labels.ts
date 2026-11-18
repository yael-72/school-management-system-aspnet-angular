import { UserRole } from './auth.models';

export const ROLE_LABELS: Readonly<Record<UserRole, string>> = {
  Admin: 'מנהל בית הספר',
  Teacher: 'מורה',
};
