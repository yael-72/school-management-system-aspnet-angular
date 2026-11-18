import { Routes } from '@angular/router';
import { AdminPage } from './admin-page';

/** /admin/... – staff, subjects and teacher assignments. Admin only (roleGuard in app.routes.ts). */
export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    component: AdminPage,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'users' },
      {
        path: 'users',
        title: 'ניהול משתמשים',
        loadComponent: () => import('./users/users-admin-page').then((m) => m.UsersAdminPage),
      },
      {
        path: 'subjects',
        title: 'ניהול מקצועות',
        loadComponent: () =>
          import('./subjects/subjects-admin-page').then((m) => m.SubjectsAdminPage),
      },
      {
        path: 'assignments',
        title: 'שיבוץ מורים',
        loadComponent: () =>
          import('./assignments/assignments-admin-page').then((m) => m.AssignmentsAdminPage),
      },
    ],
  },
];
