import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guards';
import { MainLayout } from './layout/main-layout/main-layout';

// Every feature page is lazy-loaded (loadComponent), so its code is downloaded only
// when the user first opens it. This keeps the initial bundle small.
export const routes: Routes = [
  {
    path: 'login',
    title: 'התחברות',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login/login-page').then((m) => m.LoginPage),
  },
  {
    path: '',
    component: MainLayout,
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        title: 'לוח בקרה',
        loadComponent: () => import('./features/dashboard/dashboard-page').then((m) => m.DashboardPage),
      },
      {
        path: 'classes',
        title: 'כיתות',
        loadComponent: () => import('./features/classes/classes-page').then((m) => m.ClassesPage),
      },
      {
        path: 'students',
        title: 'תלמידים',
        loadComponent: () => import('./features/students/students-page').then((m) => m.StudentsPage),
      },
      {
        path: 'attendance',
        title: 'נוכחות',
        loadComponent: () => import('./features/attendance/attendance-page').then((m) => m.AttendancePage),
      },
      {
        path: 'assessments',
        title: 'מבחנים וציונים',
        loadComponent: () => import('./features/assessments/assessments-page').then((m) => m.AssessmentsPage),
      },
      {
        path: 'profile',
        title: 'פרופיל',
        loadComponent: () => import('./features/profile/profile-page').then((m) => m.ProfilePage),
      },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
