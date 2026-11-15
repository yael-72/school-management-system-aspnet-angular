import { Component, computed, inject, signal } from '@angular/core';
import { rxResource, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { debounceTime } from 'rxjs';
import { SortDirection } from '../../../core/api/api.models';
import { toErrorMessage } from '../../../core/api/api-error';
import { UserRole } from '../../../core/auth/auth.models';
import { AuthService } from '../../../core/auth/auth.service';
import { ROLE_LABELS } from '../../../core/auth/role-labels';
import { UserListItem, UserSortField } from '../../../core/users/users.models';
import { UsersService } from '../../../core/users/users.service';
import { Avatar } from '../../../shared/components/avatar/avatar';
import { Dialog } from '../../../shared/components/dialog/dialog';
import { EmptyState } from '../../../shared/components/empty-state/empty-state';
import { Paginator } from '../../../shared/components/paginator/paginator';
import { Skeleton } from '../../../shared/components/skeleton/skeleton';
import { UserFormDialog } from './user-form-dialog';

const PAGE_SIZE = 15;

type StatusFilter = 'all' | 'active' | 'inactive';

/** /admin/users – staff members: add, edit, activate / deactivate and delete. */
@Component({
  selector: 'app-users-admin-page',
  imports: [Avatar, Dialog, EmptyState, Paginator, RouterLink, Skeleton, UserFormDialog],
  templateUrl: './users-admin-page.html',
  styleUrl: './users-admin-page.scss',
})
export class UsersAdminPage {
  private readonly usersService = inject(UsersService);
  private readonly authService = inject(AuthService);

  protected readonly roleLabels = ROLE_LABELS;
  protected readonly currentUserId = computed(() => this.authService.currentUser()?.id);
  protected readonly skeletonRows = Array.from({ length: 6 }, (_, i) => i);

  protected readonly page = signal(1);
  protected readonly sortBy = signal<UserSortField>('fullName');
  protected readonly sortDir = signal<SortDirection>('asc');
  protected readonly role = signal<UserRole | null>(null);
  protected readonly status = signal<StatusFilter>('all');
  protected readonly searchText = signal('');
  private readonly search = toSignal(toObservable(this.searchText).pipe(debounceTime(300)), {
    initialValue: '',
  });

  protected readonly users = rxResource({
    params: () => ({
      search: this.search(),
      role: this.role() ?? undefined,
      isActive: this.status() === 'all' ? undefined : this.status() === 'active',
      sortBy: this.sortBy(),
      sortDir: this.sortDir(),
      page: this.page(),
      pageSize: PAGE_SIZE,
    }),
    stream: ({ params }) => this.usersService.getUsers(params),
  });

  protected readonly errorMessage = computed(() =>
    this.users.error() ? toErrorMessage(this.users.error()) : null,
  );

  // Dialogs and row actions
  protected readonly formOpen = signal(false);
  protected readonly editedUserId = signal<number | null>(null);
  protected readonly userToDelete = signal<UserListItem | null>(null);
  protected readonly busyUserId = signal<number | null>(null);
  protected readonly actionError = signal<string | null>(null);

  protected onSearch(text: string): void {
    this.searchText.set(text);
    this.page.set(1);
  }

  protected filterByRole(value: string): void {
    this.role.set(value === 'Admin' || value === 'Teacher' ? value : null);
    this.page.set(1);
  }

  protected filterByStatus(value: string): void {
    this.status.set(value === 'active' || value === 'inactive' ? value : 'all');
    this.page.set(1);
  }

  protected sort(field: UserSortField): void {
    if (this.sortBy() === field) {
      this.sortDir.update((dir) => (dir === 'asc' ? 'desc' : 'asc'));
    } else {
      this.sortBy.set(field);
      this.sortDir.set(field === 'assignmentCount' ? 'desc' : 'asc');
    }
    this.page.set(1);
  }

  protected sortIcon(field: UserSortField): string {
    if (this.sortBy() !== field) {
      return 'unfold_more';
    }
    return this.sortDir() === 'asc' ? 'arrow_upward' : 'arrow_downward';
  }

  protected ariaSort(field: UserSortField): 'ascending' | 'descending' | null {
    if (this.sortBy() !== field) {
      return null;
    }
    return this.sortDir() === 'asc' ? 'ascending' : 'descending';
  }

  protected openCreate(): void {
    this.editedUserId.set(null);
    this.formOpen.set(true);
  }

  protected openEdit(user: UserListItem): void {
    this.editedUserId.set(user.id);
    this.formOpen.set(true);
  }

  protected onSaved(): void {
    this.formOpen.set(false);
    this.users.reload();
  }

  protected toggleStatus(user: UserListItem): void {
    this.busyUserId.set(user.id);
    this.actionError.set(null);
    this.usersService.setStatus(user.id, !user.isActive).subscribe({
      next: () => {
        this.busyUserId.set(null);
        this.users.reload();
      },
      error: (error: unknown) => {
        this.busyUserId.set(null);
        this.actionError.set(toErrorMessage(error));
      },
    });
  }

  protected askDelete(user: UserListItem): void {
    this.actionError.set(null);
    this.userToDelete.set(user);
  }

  protected confirmDelete(): void {
    const user = this.userToDelete();
    if (!user) {
      return;
    }
    this.busyUserId.set(user.id);
    this.usersService.deleteUser(user.id).subscribe({
      next: () => {
        this.busyUserId.set(null);
        this.userToDelete.set(null);
        this.users.reload();
      },
      error: (error: unknown) => {
        // The most common reason: the user has history. The message suggests deactivating instead.
        this.busyUserId.set(null);
        this.userToDelete.set(null);
        this.actionError.set(toErrorMessage(error));
      },
    });
  }
}
