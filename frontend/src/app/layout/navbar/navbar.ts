import { Component, inject, input, output } from '@angular/core';
import { ThemeService } from '../../core/theme/theme.service';
import { Avatar } from '../../shared/components/avatar/avatar';

@Component({
  selector: 'app-navbar',
  imports: [Avatar],
  templateUrl: './navbar.html',
  styleUrl: './navbar.scss',
})
export class Navbar {
  readonly pageTitle = input('');
  readonly menuToggled = output<void>();

  protected readonly themeService = inject(ThemeService);

  // Placeholder until login is implemented (milestone 2).
  protected readonly userName = 'אהרן אברמסון';
}
