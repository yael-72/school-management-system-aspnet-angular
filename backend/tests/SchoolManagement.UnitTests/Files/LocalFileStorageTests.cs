using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SchoolManagement.Infrastructure.Files;
using SchoolManagement.UnitTests.TestHelpers;

namespace SchoolManagement.UnitTests.Files;

public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"storage-tests-{Guid.NewGuid():N}");
    private readonly LocalFileStorage _storage;

    public LocalFileStorageTests()
    {
        _storage = new LocalFileStorage(
            Options.Create(new FileStorageOptions { RootPath = _root }),
            new TestHostEnvironment(),
            new FixedTimeProvider(new DateTimeOffset(2026, 11, 10, 8, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public async Task Saves_under_a_generated_name_and_reads_it_back()
    {
        var path = await _storage.SaveAsync(new MemoryStream([1, 2, 3]), ".pdf");

        path.Should().MatchRegex(@"^2026/11/[0-9a-f]{32}\.pdf$");
        using var stream = _storage.OpenRead(path);
        using var copy = new MemoryStream();
        await stream.CopyToAsync(copy);
        copy.ToArray().Should().Equal(1, 2, 3);
    }

    [Theory]
    [InlineData("../outside.txt")]
    [InlineData("2026/../../outside.txt")]
    public void Paths_outside_the_root_folder_are_refused(string storedPath)
    {
        _storage.Invoking(s => s.OpenRead(storedPath)).Should().Throw<InvalidOperationException>();
        _storage.Invoking(s => s.Delete(storedPath)).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Delete_removes_the_file_and_ignores_a_missing_one()
    {
        var path = await _storage.SaveAsync(new MemoryStream([1]), ".txt");

        _storage.Delete(path);

        File.Exists(Path.Combine(_root, path)).Should().BeFalse();
        _storage.Invoking(s => s.Delete(path)).Should().NotThrow();
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
