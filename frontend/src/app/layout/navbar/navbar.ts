import { Component, ElementRef, computed, inject, input, output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { ThemeService } from '../../core/theme/theme.service';
import { Avatar } from '../../shared/components/avatar/avatar';

@Component({
  selector: 'app-navbar',
  imports: [Avatar, RouterLink],
  templateUrl: './navbar.html',
  styleUrl: './navbar.scss',
  host: {
    // Close the user menu when clicking anywhere outside the navbar.
    '(document:click)': 'closeMenuOnOutsideClick($event)',
    '(document:keydown.escape)': 'userMenuOpen.set(false)',
  },
})
export class Navbar {
  private readonly elementRef = inject(ElementRef<HTMLElement>);
  private readonly authService = inject(AuthService);
  protected readonly themeService = inject(ThemeService);

  readonly pageTitle = input('');
  readonly menuToggled = output<void>();

  protected readonly user = this.authService.currentUser;
  protected readonly userName = computed(() => this.user()?.fullName ?? '');
  protected readonly userMenuOpen = signal(false);

  protected logout(): void {
    this.userMenuOpen.set(false);
    this.authService.logout();
  }

  protected closeMenuOnOutsideClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target as Node)) {
      this.userMenuOpen.set(false);
    }
  }
}
