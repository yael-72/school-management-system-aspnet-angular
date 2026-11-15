namespace SchoolManagement.Application.Files;

/// <summary>
/// The logged-in user's uploaded files (profile page). Files are private: another user's file is
/// reported as not found (404).
/// </summary>
public interface IFileService
{
    /// <summary>The user's files, newest first, with her storage usage.</summary>
    Task<FilesOverviewDto> GetFilesAsync(CancellationToken cancellationToken = default);

    /// <summary>Checks the size, type and quota limits (see FileRules), then stores the file.</summary>
    Task<UploadedFileDto> UploadAsync(FileUpload upload, CancellationToken cancellationToken = default);

    Task<FileDownload> DownloadAsync(int id, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
