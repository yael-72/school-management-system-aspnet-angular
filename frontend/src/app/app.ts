import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ThemeService } from './core/theme/theme.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: '<router-outlet />',
})
export class App {
  // Injected here so the saved theme is applied as soon as the app starts.
  private readonly themeService = inject(ThemeService);
}
