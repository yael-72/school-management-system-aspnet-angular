import { HttpClient, HttpEvent } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { FILE_RULES, FilesOverview, UploadedFile } from './profile.models';
import { formatBytes } from '../../shared/utils/format-bytes';

/** The user's private files (/api/files). */
@Injectable({ providedIn: 'root' })
export class FilesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/files';

  getFiles(): Observable<FilesOverview> {
    return this.http.get<FilesOverview>(this.baseUrl);
  }

  /** Uploads one file. Emits progress events, so the page can show a progress bar. */
  upload(file: File): Observable<HttpEvent<UploadedFile>> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<UploadedFile>(this.baseUrl, form, {
      reportProgress: true,
      observe: 'events',
    });
  }

  /**
   * Downloads a file and saves it with its original name. A plain link cannot be used, because the
   * API needs the JWT header (added by the auth interceptor only to HttpClient requests).
   */
  download(file: UploadedFile): Observable<void> {
    return this.http.get(`${this.baseUrl}/${file.id}/download`, { responseType: 'blob' }).pipe(
      map((blob) => {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = file.fileName;
        link.click();
        URL.revokeObjectURL(url);
      }),
    );
  }

  deleteFile(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}

/** Checks a file against FILE_RULES before uploading. Returns a Hebrew message, or null when it is fine. */
export function validateFile(file: File): string | null {
  const dot = file.name.lastIndexOf('.');
  const extension = dot >= 0 ? file.name.slice(dot).toLowerCase() : '';
  if (!(FILE_RULES.allowedExtensions as readonly string[]).includes(extension)) {
    return 'סוג הקובץ אינו נתמך. ניתן להעלות מסמכים (PDF, Word, Excel, PowerPoint, טקסט) ותמונות.';
  }
  if (file.size === 0) {
    return 'הקובץ ריק.';
  }
  if (file.size > FILE_RULES.maxFileSizeBytes) {
    return `הקובץ גדול מדי. הגודל המרבי הוא ${formatBytes(FILE_RULES.maxFileSizeBytes)}.`;
  }
  return null;
}
