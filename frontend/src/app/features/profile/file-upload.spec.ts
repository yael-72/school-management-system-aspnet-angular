import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { FileUpload } from './file-upload';

describe('FileUpload', () => {
  let http: HttpTestingController;

  async function chooseFile(file: File) {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(FileUpload);
    await fixture.whenStable();

    const input = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>(
      'input[type="file"]',
    )!;
    Object.defineProperty(input, 'files', { value: [file] });
    input.dispatchEvent(new Event('change'));
    return fixture;
  }

  afterEach(() => http.verify());

  it('rejects a file type the API does not accept, without uploading it', async () => {
    const fixture = await chooseFile(new File(['x'], 'setup.exe'));
    await fixture.whenStable();

    http.expectNone('/api/files');
    expect(
      (fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent,
    ).toContain('סוג הקובץ');
  });

  it('uploads an allowed file as multipart form data and reports it', async () => {
    const fixture = await chooseFile(new File(['hello'], 'notes.txt'));
    const uploaded: string[] = [];
    fixture.componentInstance.uploaded.subscribe((file) => uploaded.push(file.fileName));

    const request = http.expectOne('/api/files');
    expect((request.request.body as FormData).get('file')).toBeInstanceOf(File);
    request.flush({ id: 1, fileName: 'notes.txt', sizeBytes: 5, uploadedAt: '' });
    await fixture.whenStable();

    expect(uploaded).toEqual(['notes.txt']);
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('הועלה');
  });
});
