import { Component, computed, inject, input, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { NAV_ITEMS } from '../../core/navigation/nav-items';

@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.scss',
  host: { '[class.is-open]': 'open()' },
})
export class Sidebar {
  /** Only used on small screens, where the sidebar slides in over the content. */
  readonly open = input(false);
  readonly closed = output<void>();

  private readonly authService = inject(AuthService);

  /** The links this user may open; for example "ניהול" only for admins. */
  protected readonly navItems = computed(() =>
    NAV_ITEMS.filter((item) => !item.roles || this.authService.hasRole(...item.roles)),
  );
}
