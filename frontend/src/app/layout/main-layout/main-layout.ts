import { Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { Navbar } from '../navbar/navbar';
import { Sidebar } from '../sidebar/sidebar';

/**
 * The app shell: sidebar + navbar around the current page (<router-outlet>).
 * All pages that need a logged-in user are children of this layout in app.routes.ts.
 */
@Component({
  selector: 'app-main-layout',
  imports: [RouterOutlet, Sidebar, Navbar],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.scss',
})
export class MainLayout {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly sidebarOpen = signal(false);

  /** The `title` of the active route, shown in the navbar. */
  protected readonly pageTitle = toSignal(
    this.router.events.pipe(
      filter((event) => event instanceof NavigationEnd),
      map(() => this.getActiveRouteTitle()),
    ),
    { initialValue: this.getActiveRouteTitle() },
  );

  private getActiveRouteTitle(): string {
    let route = this.route.snapshot;
    while (route.firstChild) {
      route = route.firstChild;
    }
    return typeof route.title === 'string' ? route.title : '';
  }
}
