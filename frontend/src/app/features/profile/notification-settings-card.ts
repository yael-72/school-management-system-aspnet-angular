import { Component, computed, inject, linkedSignal, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { toErrorMessage } from '../../core/api/api-error';
import { Skeleton } from '../../shared/components/skeleton/skeleton';
import { Toggle } from '../../shared/components/toggle/toggle';
import { NotificationSettings } from './profile.models';
import { ProfileService } from './profile.service';

const OPTIONS: readonly { key: keyof NotificationSettings; label: string }[] = [
  { key: 'remindBeforeExam', label: 'תזכורת יום לפני מבחן' },
  { key: 'alertOnLowAttendance', label: 'התראה על היעדרויות תכופות' },
  { key: 'weeklySummaryEmail', label: 'סיכום שבועי במייל' },
];

/**
 * Notification toggles (Horizon "Notifications" card). Each change is saved at once: the switch
 * moves immediately, and moves back with a message if the save fails.
 */
@Component({
  selector: 'app-notification-settings-card',
  imports: [Skeleton, Toggle],
  template: `
    <h2 class="settings-title">התראות</h2>
    @if (loadError(); as message) {
      <p class="form-alert" role="alert">{{ message }}</p>
    } @else if (current(); as settings) {
      <ul class="settings-list">
        @for (option of options; track option.key) {
          <li>
            <app-toggle
              [label]="option.label"
              [checked]="settings[option.key]"
              [disabled]="saving()"
              (changed)="change(option.key, $event)"
            />
          </li>
        }
      </ul>
      @if (saveError(); as message) {
        <p class="form-alert settings-error" role="alert">{{ message }}</p>
      }
    } @else {
      <app-skeleton height="120px" />
    }
  `,
  styles: `
    .settings-title {
      margin-block-end: var(--space-5);
      font-size: var(--font-size-lg);
      font-weight: 700;
    }
    .settings-list {
      display: flex;
      flex-direction: column;
      gap: var(--space-5);
      padding: 0;
      margin: 0;
      list-style: none;
    }
    .settings-error {
      margin-block-start: var(--space-4);
    }
  `,
})
export class NotificationSettingsCard {
  private readonly profileService = inject(ProfileService);

  protected readonly options = OPTIONS;

  private readonly settings = rxResource({
    stream: () => this.profileService.getNotificationSettings(),
  });

  /** What the switches show: the loaded settings, then each change the user made. */
  protected readonly current = linkedSignal(() => this.settings.value());

  protected readonly loadError = computed(() =>
    this.settings.error() ? toErrorMessage(this.settings.error()) : null,
  );
  protected readonly saving = signal(false);
  protected readonly saveError = signal<string | null>(null);

  protected change(key: keyof NotificationSettings, value: boolean): void {
    const previous = this.current();
    if (!previous) {
      return;
    }
    const updated = { ...previous, [key]: value };
    this.current.set(updated);
    this.saving.set(true);
    this.saveError.set(null);

    this.profileService.updateNotificationSettings(updated).subscribe({
      next: (saved) => {
        this.saving.set(false);
        this.current.set(saved);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.current.set(previous); // undo the switch
        this.saveError.set(toErrorMessage(error));
      },
    });
  }
}
