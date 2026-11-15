using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Common.Exceptions;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Domain.Entities;

namespace SchoolManagement.Application.Files;

public class FileService(
    IApplicationDbContext db,
    IFileStorage storage,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    ILogger<FileService> logger) : IFileService
{
    private const int MaxFileNameLength = 200;

    public async Task<FilesOverviewDto> GetFilesAsync(CancellationToken cancellationToken = default)
    {
        var files = await MyFiles()
            .AsNoTracking()
            .OrderByDescending(f => f.UploadedAt)
            .ThenByDescending(f => f.Id)
            .Select(f => new UploadedFileDto(f.Id, f.FileName, f.SizeBytes, f.UploadedAt))
            .ToListAsync(cancellationToken);

        return new FilesOverviewDto(files, files.Sum(f => f.SizeBytes), FileRules.QuotaBytes);
    }

    public async Task<UploadedFileDto> UploadAsync(FileUpload upload, CancellationToken cancellationToken = default)
    {
        // Keep only the name: a browser may send a full path ("C:\Users\...\file.pdf").
        var fileName = Path.GetFileName(upload.FileName.Replace('\\', '/')).Trim();
        var extension = Path.GetExtension(fileName);

        if (fileName.Length == 0 || fileName.Length > MaxFileNameLength)
        {
            throw new BusinessRuleException($"The file name must be 1 to {MaxFileNameLength} characters long.");
        }

        if (!FileRules.AllowedExtensions.Contains(extension))
        {
            throw new BusinessRuleException(
                $"This file type is not allowed. Allowed types: {string.Join(", ", FileRules.AllowedExtensions)}.");
        }

        if (upload.SizeBytes <= 0)
        {
            throw new BusinessRuleException("The file is empty.");
        }

        if (upload.SizeBytes > FileRules.MaxFileSizeBytes)
        {
            throw new BusinessRuleException($"A file can be at most {FileRules.MaxFileSizeBytes / (1024 * 1024)} MB.");
        }

        var usedBytes = await MyFiles().SumAsync(f => f.SizeBytes, cancellationToken);
        if (usedBytes + upload.SizeBytes > FileRules.QuotaBytes)
        {
            throw new BusinessRuleException(
                $"Not enough storage space: you can keep up to {FileRules.QuotaBytes / (1024 * 1024)} MB of files.");
        }

        // The content is written first under a generated name; the user's file name is only metadata.
        var storedPath = await storage.SaveAsync(upload.Content, extension.ToLowerInvariant(), cancellationToken);

        var file = new UploadedFile
        {
            UserId = currentUser.Id,
            FileName = fileName,
            StoredPath = storedPath,
            SizeBytes = upload.SizeBytes,
            UploadedAt = timeProvider.GetUtcNow().UtcDateTime
        };
        db.UploadedFiles.Add(file);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            storage.Delete(storedPath); // do not leave an orphan file on disk
            throw;
        }

        logger.LogInformation("User {UserId} uploaded file {FileId} ({SizeBytes} bytes)", currentUser.Id, file.Id, file.SizeBytes);
        return new UploadedFileDto(file.Id, file.FileName, file.SizeBytes, file.UploadedAt);
    }

    public async Task<FileDownload> DownloadAsync(int id, CancellationToken cancellationToken = default)
    {
        var file = await FindMyFileAsync(id, cancellationToken);
        return new FileDownload(file.FileName, storage.OpenRead(file.StoredPath));
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var file = await FindMyFileAsync(id, cancellationToken);

        db.UploadedFiles.Remove(file);
        await db.SaveChangesAsync(cancellationToken);
        storage.Delete(file.StoredPath);

        logger.LogInformation("User {UserId} deleted file {FileId}", currentUser.Id, id);
    }

    private IQueryable<UploadedFile> MyFiles() => db.UploadedFiles.Where(f => f.UserId == currentUser.Id);

    private async Task<UploadedFile> FindMyFileAsync(int id, CancellationToken cancellationToken) =>
        await MyFiles().SingleOrDefaultAsync(f => f.Id == id, cancellationToken)
        ?? throw new NotFoundException("File", id);
}
