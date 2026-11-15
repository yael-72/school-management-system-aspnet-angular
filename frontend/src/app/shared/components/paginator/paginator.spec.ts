import { TestBed } from '@angular/core/testing';
import { Paginator } from './paginator';

describe('Paginator', () => {
  async function render(page: number, totalCount: number) {
    const fixture = TestBed.createComponent(Paginator);
    fixture.componentRef.setInput('page', page);
    fixture.componentRef.setInput('pageSize', 20);
    fixture.componentRef.setInput('totalCount', totalCount);
    await fixture.whenStable();
    return fixture;
  }

  it('shows the range of the current page', async () => {
    const fixture = await render(2, 45);
    const element = fixture.nativeElement as HTMLElement;

    expect(element.textContent).toContain('מציג 21–40 מתוך 45');
    expect(element.textContent).toContain('עמוד 2 מתוך 3');
  });

  it('disables "next" on the last page and emits the previous page number', async () => {
    const fixture = await render(3, 45);
    const emitted: number[] = [];
    fixture.componentInstance.pageChange.subscribe((page) => emitted.push(page));
    const [previous, next] = (fixture.nativeElement as HTMLElement).querySelectorAll('button');

    expect(next.disabled).toBe(true);
    previous.click();
    expect(emitted).toEqual([2]);
  });
});
