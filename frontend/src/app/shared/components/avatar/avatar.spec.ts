import { TestBed } from '@angular/core/testing';
import { Avatar } from './avatar';

describe('Avatar', () => {
  it('shows the first letter of the first two name parts', async () => {
    const fixture = TestBed.createComponent(Avatar);
    fixture.componentRef.setInput('name', 'רחל אברמסון');
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent?.trim()).toBe('רא');
  });
});
