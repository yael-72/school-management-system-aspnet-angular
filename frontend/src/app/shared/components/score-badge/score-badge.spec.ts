import { scoreLevel } from './score-badge';

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
