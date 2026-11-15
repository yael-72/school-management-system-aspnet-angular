using SchoolManagement.Application.Common.Interfaces;

namespace SchoolManagement.UnitTests.TestHelpers;

/// <summary>Keeps "stored" files in a dictionary, so file service tests do not touch the disk.</summary>
public class InMemoryFileStorage : IFileStorage
{
    public Dictionary<string, byte[]> Files { get; } = [];

    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default)
    {
        using var memory = new MemoryStream();
        await content.CopyToAsync(memory, cancellationToken);
        var path = $"{Guid.NewGuid():N}{extension}";
        Files[path] = memory.ToArray();
        return path;
    }

    public Stream OpenRead(string storedPath) => new MemoryStream(Files[storedPath]);

    public void Delete(string storedPath) => Files.Remove(storedPath);
}
