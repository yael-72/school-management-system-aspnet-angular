import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { toErrorMessage } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { Avatar } from '../../shared/components/avatar/avatar';
import { Card } from '../../shared/components/card/card';
import { Dialog } from '../../shared/components/dialog/dialog';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { Skeleton } from '../../shared/components/skeleton/skeleton';
import { formatBytes } from '../../shared/utils/format-bytes';
import { FileUpload } from './file-upload';
import { FilesService } from './files.service';
import { NotificationSettingsCard } from './notification-settings-card';
import { PersonalDetails } from './personal-details';
import { Profile, UploadedFile } from './profile.models';
import { ProfileService } from './profile.service';
import { ROLE_LABELS } from './role-labels';

/** Icon per file type in the files list. */
function fileIcon(fileName: string): string {
  const extension = fileName.slice(fileName.lastIndexOf('.') + 1).toLowerCase();
  if (['png', 'jpg', 'jpeg', 'gif'].includes(extension)) return 'image';
  if (['xls', 'xlsx', 'csv'].includes(extension)) return 'table_chart';
  if (['ppt', 'pptx'].includes(extension)) return 'slideshow';
  if (extension === 'pdf') return 'picture_as_pdf';
  return 'description';
}

/** /profile – the user's details, files and notification settings (Horizon "Profile"). */
@Component({
  selector: 'app-profile-page',
  imports: [
    Avatar,
    Card,
    DatePipe,
    Dialog,
    EmptyState,
    FileUpload,
    NotificationSettingsCard,
    PersonalDetails,
    Skeleton,
  ],
  templateUrl: './profile-page.html',
  styleUrl: './profile-page.scss',
})
export class ProfilePage {
  private readonly profileService = inject(ProfileService);
  private readonly filesService = inject(FilesService);
  private readonly authService = inject(AuthService);

  protected readonly roleLabels = ROLE_LABELS;
  protected readonly formatBytes = formatBytes;
  protected readonly fileIcon = fileIcon;

  protected readonly profile = rxResource({ stream: () => this.profileService.getProfile() });
  protected readonly files = rxResource({ stream: () => this.filesService.getFiles() });

  protected readonly profileError = computed(() =>
    this.profile.error() ? toErrorMessage(this.profile.error()) : null,
  );
  protected readonly filesError = computed(() =>
    this.files.error() ? toErrorMessage(this.files.error()) : null,
  );

  /** Share of the storage quota in use, 0–100. */
  protected readonly usedPercent = computed(() => {
    const overview = this.files.value();
    return overview ? Math.min(100, (overview.usedBytes * 100) / overview.quotaBytes) : 0;
  });

  protected readonly fileError = signal<string | null>(null);
  protected readonly fileToDelete = signal<UploadedFile | null>(null);
  protected readonly deleting = signal(false);

  protected onProfileSaved(profile: Profile): void {
    this.profile.set(profile);
    this.authService.updateCurrentUser({ fullName: profile.fullName }); // the navbar shows the new name
  }

  protected download(file: UploadedFile): void {
    this.fileError.set(null);
    this.filesService.download(file).subscribe({
      error: (error: unknown) => this.fileError.set(toErrorMessage(error)),
    });
  }

  protected confirmDelete(): void {
    const file = this.fileToDelete();
    if (!file) {
      return;
    }
    this.deleting.set(true);
    this.filesService.deleteFile(file.id).subscribe({
      next: () => {
        this.deleting.set(false);
        this.fileToDelete.set(null);
        this.files.reload();
      },
      error: (error: unknown) => {
        this.deleting.set(false);
        this.fileToDelete.set(null);
        this.fileError.set(toErrorMessage(error));
      },
    });
  }
}
