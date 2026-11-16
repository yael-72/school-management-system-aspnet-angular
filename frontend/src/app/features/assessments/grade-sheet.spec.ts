import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AssessmentScores, SaveScoresRequest } from './assessments.models';
import { GradeSheet } from './grade-sheet';

describe('GradeSheet', () => {
  const sheet: AssessmentScores = {
    assessment: {
      id: 7,
      title: 'בוחן שברים',
      type: 'Quiz',
      classId: 3,
      className: "ז'1",
      subjectId: 1,
      subjectName: 'מתמטיקה',
      date: '2026-01-01',
      maxScore: 20,
      createdByName: 'משה לוינסון',
      canEdit: true,
    },
    gradedCount: 1,
    averageScore: 90,
    highestScore: 18,
    lowestScore: 18,
    students: [
      { studentId: 1, firstName: 'יוסף', lastName: 'אברהם', score: 18, percent: 90, comment: null },
      {
        studentId: 2,
        firstName: 'דוד',
        lastName: 'ברק',
        score: null,
        percent: null,
        comment: null,
      },
    ],
  };

  let http: HttpTestingController;

  async function render(data: AssessmentScores = sheet) {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(GradeSheet);
    fixture.componentRef.setInput('sheet', data);
    await fixture.whenStable();
    return { fixture, element: fixture.nativeElement as HTMLElement };
  }

  function saveButton(element: HTMLElement): HTMLButtonElement {
    return Array.from(element.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('שמירת ציונים'),
    )!;
  }

  async function typeScore(
    fixture: { whenStable(): Promise<unknown> },
    input: HTMLInputElement,
    value: string,
  ) {
    input.value = value;
    input.dispatchEvent(new Event('input'));
    await fixture.whenStable();
  }

  afterEach(() => http.verify());

  it('shows each score as a color-coded percentage', async () => {
    const { element } = await render();

    const badges = element.querySelectorAll('app-score-badge app-status-badge');
    expect(badges[0].textContent?.trim()).toBe('90');
    expect(badges[0].getAttribute('data-tone')).toBe('success');
    expect(saveButton(element).disabled).toBe(true); // nothing changed yet
  });

  it('sends only the changed rows', async () => {
    const { fixture, element } = await render();
    const inputs = element.querySelectorAll<HTMLInputElement>('input.sheet-score');

    await typeScore(fixture, inputs[1], '12.5');
    expect(element.textContent).toContain('62.5'); // live percentage: 12.5 / 20
    saveButton(element).click();

    const request = http.expectOne('/api/assessments/7/scores');
    expect((request.request.body as SaveScoresRequest).scores).toEqual([
      { studentId: 2, score: 12.5, comment: null },
    ]);
    request.flush(sheet);
    await fixture.whenStable();

    expect(element.textContent).toContain('הציונים נשמרו');
  });

  it('blocks saving a score above the max score', async () => {
    const { fixture, element } = await render();
    const input = element.querySelector<HTMLInputElement>('input.sheet-score')!;

    await typeScore(fixture, input, '25');

    expect(input.classList).toContain('is-invalid');
    expect(element.querySelector('[role="alert"]')?.textContent).toContain('בין 0 ל-20');
    expect(saveButton(element).disabled).toBe(true);
  });

  it('is read-only for a user who may not grade the subject', async () => {
    const { element } = await render({
      ...sheet,
      assessment: { ...sheet.assessment, canEdit: false },
    });

    expect(element.querySelector('input')).toBeNull();
    expect(element.textContent).toContain('צפייה בלבד');
  });

  it('does not allow grading before the assessment date', async () => {
    const future = { ...sheet.assessment, date: '9999-12-31' };

    const { element } = await render({ ...sheet, assessment: future });

    expect(element.querySelector('input')).toBeNull();
    expect(element.textContent).toContain('המבחן עוד לא התקיים');
  });
});
