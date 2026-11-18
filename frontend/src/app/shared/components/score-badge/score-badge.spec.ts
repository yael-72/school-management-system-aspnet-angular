import { TestBed } from '@angular/core/testing';
import { ScoreBadge, scoreLevel } from './score-badge';

describe('scoreLevel', () => {
  it.each([
    [95, 'excellent'],
    [90, 'excellent'],
    [80, 'good'],
    [60, 'fair'],
    [40, 'low'],
  ])('%d is %s', (score, level) => {
    expect(scoreLevel(score)).toBe(level);
  });
});

describe('ScoreBadge', () => {
  async function render(score: number | null): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(ScoreBadge);
    fixture.componentRef.setInput('score', score);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('shows the score in the color of its grade band', async () => {
    const badge = (await render(92.25)).querySelector('app-status-badge')!;

    expect(badge.textContent?.trim()).toBe('92.3');
    expect(badge.getAttribute('data-tone')).toBe('success');
  });

  it('shows a dash when there is no score', async () => {
    expect((await render(null)).textContent?.trim()).toBe('—');
  });
});
