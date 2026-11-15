using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolManagement.Application.Common.Exceptions;
using SchoolManagement.Application.Files;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.UnitTests.TestHelpers;

namespace SchoolManagement.UnitTests.Files;

public class FileServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 8, 0, 0, TimeSpan.Zero);

    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private readonly InMemoryFileStorage _storage = new();

    private FileService ServiceFor(int userId) =>
        new(_db, _storage, FakeCurrentUser.Teacher(userId), new FixedTimeProvider(Now), NullLogger<FileService>.Instance);

    private static FileUpload Upload(string fileName, string content = "hello") =>
        new(fileName, Encoding.UTF8.GetByteCount(content), new MemoryStream(Encoding.UTF8.GetBytes(content)));

    /// <summary>An upload that claims a size without allocating it (the service checks the size before reading).</summary>
    private static FileUpload UploadOfSize(string fileName, long sizeBytes) => new(fileName, sizeBytes, new MemoryStream([1]));

    [Fact]
    public async Task Upload_stores_the_content_and_keeps_only_the_file_name()
    {
        var service = ServiceFor(1);

        var uploaded = await service.UploadAsync(Upload(@"C:\Users\moshe\Documents\מערכת שעות.pdf"));

        uploaded.FileName.Should().Be("מערכת שעות.pdf");
        var file = await _db.UploadedFiles.SingleAsync();
        file.StoredPath.Should().EndWith(".pdf").And.NotContain("מערכת");
        _storage.Files[file.StoredPath].Should().Equal(Encoding.UTF8.GetBytes("hello"));
    }

    [Theory]
    [InlineData("script.exe")]
    [InlineData("page.html")]
    [InlineData("no-extension")]
    public async Task Disallowed_file_types_are_rejected(string fileName)
    {
        var act = () => ServiceFor(1).UploadAsync(Upload(fileName));

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*file type is not allowed*");
        _storage.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task A_file_bigger_than_the_limit_is_rejected()
    {
        var act = () => ServiceFor(1).UploadAsync(UploadOfSize("big.pdf", FileRules.MaxFileSizeBytes + 1));

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*at most 5 MB*");
    }

    [Fact]
    public async Task Upload_beyond_the_storage_quota_is_rejected()
    {
        var service = ServiceFor(1);
        for (var i = 0; i < 10; i++)
        {
            await service.UploadAsync(UploadOfSize($"file{i}.pdf", FileRules.MaxFileSizeBytes)); // 10 × 5 MB = 50 MB
        }

        var act = () => service.UploadAsync(Upload("one-more.txt"));

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*Not enough storage space*");
        (await service.GetFilesAsync()).UsedBytes.Should().Be(FileRules.QuotaBytes);
    }

    [Fact]
    public async Task Files_are_private_to_their_owner()
    {
        var mine = await ServiceFor(1).UploadAsync(Upload("שלי.txt"));
        var stranger = ServiceFor(2);

        (await stranger.GetFilesAsync()).Files.Should().BeEmpty();
        await stranger.Invoking(s => s.DownloadAsync(mine.Id)).Should().ThrowAsync<NotFoundException>();
        await stranger.Invoking(s => s.DeleteAsync(mine.Id)).Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Delete_removes_the_record_and_the_stored_content()
    {
        var service = ServiceFor(1);
        var uploaded = await service.UploadAsync(Upload("notes.txt"));

        await service.DeleteAsync(uploaded.Id);

        (await _db.UploadedFiles.AnyAsync()).Should().BeFalse();
        _storage.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task Download_returns_the_original_name_and_content()
    {
        var service = ServiceFor(1);
        var uploaded = await service.UploadAsync(Upload("notes.txt", "שלום"));

        var download = await service.DownloadAsync(uploaded.Id);

        download.FileName.Should().Be("notes.txt");
        new StreamReader(download.Content).ReadToEnd().Should().Be("שלום");
    }
}
