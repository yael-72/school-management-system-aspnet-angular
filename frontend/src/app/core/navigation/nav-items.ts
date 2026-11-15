/** One link in the sidebar. `icon` is a Material Symbols icon name. */
export interface NavItem {
  label: string;
  path: string;
  icon: string;
}

export const NAV_ITEMS: readonly NavItem[] = [
  { label: 'לוח בקרה', path: '/dashboard', icon: 'home' },
  { label: 'כיתות', path: '/classes', icon: 'school' },
  { label: 'תלמידים', path: '/students', icon: 'groups' },
  { label: 'נוכחות', path: '/attendance', icon: 'fact_check' },
  { label: 'מבחנים וציונים', path: '/assessments', icon: 'assignment' },
  { label: 'פרופיל', path: '/profile', icon: 'person' },
];
