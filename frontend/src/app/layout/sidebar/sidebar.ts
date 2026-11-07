import { Component, input, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
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

  protected readonly navItems = NAV_ITEMS;
}
