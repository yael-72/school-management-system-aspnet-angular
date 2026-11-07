import { TestBed } from '@angular/core/testing';
import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
  });

  it('uses the theme saved in localStorage', () => {
    localStorage.setItem('theme', 'dark');

    const service = TestBed.inject(ThemeService);

    expect(service.theme()).toBe('dark');
  });

  it('toggle switches between light and dark and applies it to <html>', () => {
    localStorage.setItem('theme', 'light');
    const service = TestBed.inject(ThemeService);

    service.toggle();
    TestBed.tick(); // let the effect run

    expect(service.theme()).toBe('dark');
    expect(document.documentElement.dataset['theme']).toBe('dark');
    expect(localStorage.getItem('theme')).toBe('dark');
  });
});
