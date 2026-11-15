namespace SchoolManagement.Application.Common.Interfaces;

/// <summary>
/// Where uploaded file contents are kept. The services only know this interface, so the local-disk
/// implementation (Infrastructure) could be replaced by cloud storage without touching them.
/// </summary>
public interface IFileStorage
{
    /// <summary>Stores the content under a new, server-generated name and returns that name.</summary>
    Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default);

    /// <summary>Opens a stored file for reading. The caller disposes the stream.</summary>
    Stream OpenRead(string storedPath);

    /// <summary>Deletes a stored file; does nothing when it is already gone.</summary>
    void Delete(string storedPath);
}
