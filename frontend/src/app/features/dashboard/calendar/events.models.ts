/** EventDto in the API. */
export interface SchoolEvent {
  id: number;
  title: string;
  /** ISO date "yyyy-MM-dd". */
  startDate: string;
  /** Last day of a multi-day event, or null for a one-day event. */
  endDate: string | null;
  /** Null for a school-wide event. */
  classId: number | null;
  className: string | null;
  canEdit: boolean;
}

/** Body of POST /api/events and PUT /api/events/{id}. */
export interface SaveEventRequest {
  title: string;
  startDate: string;
  endDate: string | null;
  classId: number | null;
}
