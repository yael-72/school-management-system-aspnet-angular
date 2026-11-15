import { validateFile } from './files.service';

function fakeFile(name: string, size: number): File {
  // A File whose reported size we control, without allocating megabytes in the test.
  const file = new File(['x'], name);
  Object.defineProperty(file, 'size', { value: size });
  return file;
}

describe('validateFile', () => {
  it('accepts documents and images within the size limit', () => {
    expect(validateFile(fakeFile('מערכת שעות.PDF', 1000))).toBeNull();
    expect(validateFile(fakeFile('photo.jpeg', 1000))).toBeNull();
  });

  it('rejects other types, empty files and files over 5 MB', () => {
    expect(validateFile(fakeFile('setup.exe', 1000))).toContain('סוג הקובץ');
    expect(validateFile(fakeFile('notes.txt', 0))).toContain('ריק');
    expect(validateFile(fakeFile('big.pdf', 5 * 1024 * 1024 + 1))).toContain('5 MB');
  });
});
