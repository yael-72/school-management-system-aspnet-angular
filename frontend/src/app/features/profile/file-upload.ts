import { HttpEventType } from '@angular/common/http';
import { Component, inject, output, signal } from '@angular/core';
import { toErrorMessage } from '../../core/api/api-error';
import { FILE_RULES, UploadedFile } from './profile.models';
import { FilesService, validateFile } from './files.service';

/**
 * Drop zone for uploading a file (Horizon "Upload Files" card): click to choose a file, or drag one
 * onto it. Shows a progress bar while uploading.
 */
@Component({
  selector: 'app-file-upload',
  templateUrl: './file-upload.html',
  styleUrl: './file-upload.scss',
})
export class FileUpload {
  private readonly filesService = inject(FilesService);

  readonly uploaded = output<UploadedFile>();

  protected readonly accept = FILE_RULES.allowedExtensions.join(',');
  protected readonly dragging = signal(false);
  /** 0–100 while uploading, null otherwise. */
  protected readonly progress = signal<number | null>(null);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly lastUploaded = signal<string | null>(null);

  protected onFileChosen(input: HTMLInputElement): void {
    const file = input.files?.[0];
    input.value = ''; // so choosing the same file again fires "change" again
    if (file) {
      this.upload(file);
    }
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault(); // needed so the browser allows dropping here
    this.dragging.set(true);
  }

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);
    const file = event.dataTransfer?.files[0];
    if (file) {
      this.upload(file);
    }
  }

  private upload(file: File): void {
    this.lastUploaded.set(null);
    const problem = validateFile(file);
    this.errorMessage.set(problem);
    if (problem || this.progress() !== null) {
      return;
    }

    this.progress.set(0);
    this.filesService.upload(file).subscribe({
      next: (event) => {
        if (event.type === HttpEventType.UploadProgress && event.total) {
          this.progress.set(Math.round((event.loaded * 100) / event.total));
        } else if (event.type === HttpEventType.Response && event.body) {
          this.progress.set(null);
          this.lastUploaded.set(event.body.fileName);
          this.uploaded.emit(event.body);
        }
      },
      error: (error: unknown) => {
        this.progress.set(null);
        this.errorMessage.set(toErrorMessage(error));
      },
    });
  }
}
