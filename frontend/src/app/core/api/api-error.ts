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
  [
    'Cannot delete a class that still has',
    'לא ניתן למחוק כיתה שיש בה תלמידים פעילים. יש להעביר אותם לכיתה אחרת קודם.',
  ],
  ['history must be kept', 'לא ניתן למחוק כיתה שיש לה היסטוריית ציונים או תלמידים לשעבר.'],
  ['already exists in school year', 'כבר קיימת כיתה בשם הזה באותה שנת לימודים.'],
  ['Homeroom teacher', 'המחנך שנבחר לא קיים או שאינו פעיל.'],
  ['Birth date cannot be in the future', 'תאריך הלידה לא יכול להיות בעתיד.'],
  ['Class with id', 'הכיתה שנבחרה לא קיימת.'],
  ['future date', 'לא ניתן לרשום נוכחות לתאריך עתידי.'],
  [
    'not active students of this class',
    'חלק מהתלמידים כבר אינם רשומים בכיתה. רעננו את הדף ונסו שוב.',
  ],
  ['start date must be', 'תאריך ההתחלה חייב להיות לפני תאריך הסיום.'],
  ['A report can cover at most', 'ניתן להפיק דוח לתקופה של עד שנה.'],
  ['between 0 and the max score', 'כל ציון חייב להיות בין 0 לציון המרבי של המבחן.'],
  ['at most 2 decimal places', 'ניתן להזין ציון עם עד שתי ספרות אחרי הנקודה.'],
  ['before the assessment date', 'ניתן להזין ציונים רק מיום המבחן ואילך.'],
  ['lower than an existing score', 'לא ניתן להוריד את הציון המרבי מתחת לציון שכבר הוזן.'],
  ['cannot be moved to another class', 'לא ניתן להעביר לכיתה אחרת מבחן שכבר הוזנו לו ציונים.'],
  ['Subject with id', 'המקצוע שנבחר לא קיים.'],
  ['file type is not allowed', 'סוג הקובץ אינו נתמך.'],
  ['Not enough storage space', 'אין מספיק מקום אחסון. מחקו קבצים ישנים ונסו שוב.'],
  ['A file can be at most', 'הקובץ גדול מדי (עד 5 MB).'],
  ['Multipart body length limit', 'הקובץ גדול מדי (עד 5 MB).'],
  ['A user with the email', 'כבר קיים משתמש עם כתובת האימייל הזאת.'],
  ['cannot change your own role', 'לא ניתן לשנות את התפקיד של עצמך.'],
  ['cannot deactivate your own account', 'לא ניתן להשבית את החשבון של עצמך.'],
  ['cannot delete your own account', 'לא ניתן למחוק את החשבון של עצמך.'],
  [
    'Deactivate the user instead',
    'למשתמש יש כיתות, שיבוצים או רישומים בבית הספר, ולכן אי אפשר למחוק אותו. אפשר להשבית אותו במקום.',
  ],
  ['a letter and a digit', 'הסיסמה חייבת להכיל 8 תווים לפחות, ובהם אות וספרה.'],
  ['A subject named', 'כבר קיים מקצוע בשם הזה.'],
  ['subject that has assessments', 'לא ניתן למחוק מקצוע שיש לו מבחנים.'],
  [
    'subject that is still taught',
    'לא ניתן למחוק מקצוע שמלמדים אותו. יש להסיר קודם את שיבוצי המורים שלו.',
  ],
  ['already teaches this subject', 'המורה כבר משובץ למקצוע הזה בכיתה הזאת.'],
  ['Teacher with id', 'המורה שנבחר לא קיים או שאינו פעיל.'],
  ['end date must be on or after', 'תאריך הסיום חייב להיות אחרי תאריך ההתחלה.'],
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
    case HttpStatusCode.PayloadTooLarge:
      return 'הקובץ גדול מדי (עד 5 MB).';
    case HttpStatusCode.BadRequest: {
      // Look in the business-rule message and in the validation messages.
      const text = [problem.detail ?? '', ...Object.values(problem.errors ?? {}).flat()].join(' ');
      const known = KNOWN_MESSAGES.find(([apiText]) => text.includes(apiText));
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
