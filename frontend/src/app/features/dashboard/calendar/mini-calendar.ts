import { Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { toErrorMessage } from '../../../core/api/api-error';
import { todayIsoDate } from '../../../shared/utils/dates';
import { Dialog } from '../../../shared/components/dialog/dialog';
import { EventFormDialog } from './event-form-dialog';
import { SchoolEvent } from './events.models';
import { EventsService } from './events.service';
import { buildMonthGrid, isOnDay } from './month-grid';

const monthTitle = new Intl.DateTimeFormat('he-IL', { month: 'long', year: 'numeric' });
const fullDate = new Intl.DateTimeFormat('he-IL', {
  weekday: 'long',
  day: 'numeric',
  month: 'long',
});

/** Sunday first, as in Israeli calendars. */
const WEEKDAYS = ['א׳', 'ב׳', 'ג׳', 'ד׳', 'ה׳', 'ו׳', 'ש׳'];

/**
 * A month calendar (Horizon mini calendar) with dots on days that have events, and the events of
 * the selected day under it. Events of the visible month are loaded with one request.
 */
@Component({
  selector: 'app-mini-calendar',
  imports: [Dialog, EventFormDialog],
  templateUrl: './mini-calendar.html',
  styleUrl: './mini-calendar.scss',
})
export class MiniCalendar {
  private readonly eventsService = inject(EventsService);

  protected readonly weekdays = WEEKDAYS;
  protected readonly today = todayIsoDate();

  /** The first day of the visible month. */
  protected readonly month = signal(firstOfMonth(new Date()));
  protected readonly selectedDate = signal(this.today);

  protected readonly title = computed(() => monthTitle.format(this.month()));

  protected readonly days = computed(() =>
    buildMonthGrid(this.month().getFullYear(), this.month().getMonth()),
  );

  protected readonly events = rxResource({
    params: () => ({ from: this.days()[0].date, to: this.days().at(-1)!.date }),
    stream: ({ params }) => this.eventsService.getEvents(params.from, params.to),
  });

  /** Days that have at least one event, for the dots. */
  protected readonly busyDays = computed(() => {
    const events = this.events.value() ?? [];
    return new Set(
      this.days()
        .filter((d) => events.some((e) => isOnDay(e, d.date)))
        .map((d) => d.date),
    );
  });

  protected readonly selectedEvents = computed(() =>
    (this.events.value() ?? []).filter((e) => isOnDay(e, this.selectedDate())),
  );

  protected readonly loadError = computed(() =>
    this.events.error() ? toErrorMessage(this.events.error()) : null,
  );

  protected readonly formOpen = signal(false);
  protected readonly eventToDelete = signal<SchoolEvent | null>(null);
  protected readonly deleteError = signal<string | null>(null);

  /** "יום שלישי, 10 בנובמבר" – read by screen readers and shown above the day's events. */
  protected dayLabel(isoDate: string): string {
    const [year, month, day] = isoDate.split('-').map(Number);
    return fullDate.format(new Date(year, month - 1, day));
  }

  protected moveMonth(offset: number): void {
    const month = this.month();
    this.month.set(new Date(month.getFullYear(), month.getMonth() + offset, 1));
  }

  protected select(date: string): void {
    this.selectedDate.set(date);
  }

  protected onCreated(event: SchoolEvent): void {
    this.formOpen.set(false);
    this.selectedDate.set(event.startDate);
    const [year, month] = event.startDate.split('-').map(Number);
    this.month.set(new Date(year, month - 1, 1)); // reloads the events if the month changed
    this.events.reload();
  }

  protected confirmDelete(): void {
    const event = this.eventToDelete();
    if (!event) {
      return;
    }
    this.eventsService.deleteEvent(event.id).subscribe({
      next: () => {
        this.eventToDelete.set(null);
        this.events.reload();
      },
      error: (error: unknown) => this.deleteError.set(toErrorMessage(error)),
    });
  }

  protected askDelete(event: SchoolEvent): void {
    this.deleteError.set(null);
    this.eventToDelete.set(event);
  }
}

function firstOfMonth(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), 1);
}
