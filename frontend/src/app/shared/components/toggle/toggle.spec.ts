import { TestBed } from '@angular/core/testing';
import { Toggle } from './toggle';

describe('Toggle', () => {
  it('asks for the new value but shows only what [checked] says', async () => {
    const fixture = TestBed.createComponent(Toggle);
    fixture.componentRef.setInput('label', 'סיכום שבועי');
    fixture.componentRef.setInput('checked', false);
    await fixture.whenStable();
    const requested: boolean[] = [];
    fixture.componentInstance.changed.subscribe((value) => requested.push(value));
    const input = (fixture.nativeElement as HTMLElement).querySelector('input')!;

    input.click();
    expect(requested).toEqual([true]);
    expect(input.checked).toBe(false); // not changed until the parent says so

    fixture.componentRef.setInput('checked', true);
    await fixture.whenStable();
    expect(input.checked).toBe(true);
  });
});
