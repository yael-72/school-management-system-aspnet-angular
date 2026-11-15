import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { AuthService } from '../../core/auth/auth.service';
import { UserRole } from '../../core/auth/auth.models';
import { Sidebar } from './sidebar';

describe('Sidebar', () => {
  async function linksFor(role: UserRole): Promise<string[]> {
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient()] });
    const auth = TestBed.inject(AuthService);
    auth.hasRole = (...roles: UserRole[]) => roles.includes(role);

    const fixture = TestBed.createComponent(Sidebar);
    await fixture.whenStable();
    return Array.from(
      (fixture.nativeElement as HTMLElement).querySelectorAll('.sidebar__link'),
    ).map((link) => link.textContent!.trim());
  }

  it('shows the admin area to admins', async () => {
    expect((await linksFor('Admin')).some((link) => link.includes('ניהול'))).toBe(true);
  });

  it('hides the admin area from teachers', async () => {
    const links = await linksFor('Teacher');

    expect(links.some((link) => link.includes('ניהול'))).toBe(false);
    expect(links.some((link) => link.includes('תלמידים'))).toBe(true);
  });
});
