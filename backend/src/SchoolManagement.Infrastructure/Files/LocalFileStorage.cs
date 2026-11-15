using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SchoolManagement.Application.Common.Interfaces;

namespace SchoolManagement.Infrastructure.Files;

/// <summary>
/// Keeps uploaded files in a folder on the server's disk, as "2026/11/{guid}.pdf".
/// The name is generated here, never taken from the user, and every path is checked to stay inside
/// the root folder – so a stored path like "../../appsettings.json" can never be read or deleted.
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly string _root;
    private readonly TimeProvider _timeProvider;

    public LocalFileStorage(IOptions<FileStorageOptions> options, IHostEnvironment environment, TimeProvider timeProvider)
    {
        _root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.RootPath));
        _timeProvider = timeProvider;
    }

    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var storedPath = $"{now:yyyy}/{now:MM}/{Guid.NewGuid():N}{extension}";
        var fullPath = ToFullPath(storedPath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var file = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write);
        await content.CopyToAsync(file, cancellationToken);

        return storedPath;
    }

    public Stream OpenRead(string storedPath) =>
        new FileStream(ToFullPath(storedPath), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);

    public void Delete(string storedPath)
    {
        var fullPath = ToFullPath(storedPath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    private string ToFullPath(string storedPath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_root, storedPath));
        if (!fullPath.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The stored path points outside the upload folder.");
        }
        return fullPath;
    }
}
