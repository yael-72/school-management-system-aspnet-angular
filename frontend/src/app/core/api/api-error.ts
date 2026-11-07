import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';

/** RFC 7807 ProblemDetails as returned by the API for every error. */
interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

/**
 * The API writes its messages in English (for developers and logs). Messages a user can
 * actually trigger are translated here; anything else gets a general Hebrew message.
 * Each entry is [text the API message contains, Hebrew message].
 */
const KNOWN_MESSAGES: readonly [string, string][] = [
  ['active students', 'לא ניתן למחוק כיתה שיש בה תלמידים פעילים. יש להעביר אותם לכיתה אחרת קודם.'],
  ['history must be kept', 'לא ניתן למחוק כיתה שיש לה היסטוריית ציונים או תלמידים לשעבר.'],
  ['already exists in school year', 'כבר קיימת כיתה בשם הזה באותה שנת לימודים.'],
  ['Homeroom teacher', 'המחנך שנבחר לא קיים או שאינו פעיל.'],
  ['Birth date cannot be in the future', 'תאריך הלידה לא יכול להיות בעתיד.'],
  ['Class with id', 'הכיתה שנבחרה לא קיימת.'],
];

/** A Hebrew message for the user, for any error coming from HttpClient. */
export function toErrorMessage(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'אירעה שגיאה לא צפויה. נסו שוב.';
  }

  const problem = (error.error ?? {}) as ProblemDetails;

  switch (error.status) {
    case 0:
      return 'אין חיבור לשרת. בדקו את החיבור ונסו שוב.';
    case HttpStatusCode.Forbidden:
      return 'אין לך הרשאה לצפות או לערוך את המידע הזה.';
    case HttpStatusCode.NotFound:
      return 'הפריט המבוקש לא נמצא. ייתכן שהוא נמחק.';
    case HttpStatusCode.BadRequest: {
      const known = KNOWN_MESSAGES.find(([apiText]) => problem.detail?.includes(apiText));
      if (known) {
        return known[1];
      }
      return problem.errors
        ? 'חלק מהשדות אינם תקינים. בדקו את הטופס ונסו שוב.'
        : 'הבקשה אינה תקינה.';
    }
    default:
      return 'אירעה שגיאה בשרת. נסו שוב מאוחר יותר.';
  }
}
