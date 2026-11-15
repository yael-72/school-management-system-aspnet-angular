namespace SchoolManagement.Application.Files;

public record UploadedFileDto(int Id, string FileName, long SizeBytes, DateTime UploadedAt);

/// <summary>GET /api/files – the user's files and how much of her storage they use.</summary>
public record FilesOverviewDto(IReadOnlyList<UploadedFileDto> Files, long UsedBytes, long QuotaBytes);

/// <summary>A file on its way in (POST /api/files), without ASP.NET Core types.</summary>
public record FileUpload(string FileName, long SizeBytes, Stream Content);

/// <summary>A file on its way out (GET /api/files/{id}/download). The caller disposes the stream.</summary>
public record FileDownload(string FileName, Stream Content);

/// <summary>
/// Upload limits. Only document and image types are accepted: no executables or HTML, which a
/// browser could run if a file were ever served inline.
/// </summary>
public static class FileRules
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;
    public const long QuotaBytes = 50 * 1024 * 1024;

    public static readonly IReadOnlySet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".csv", ".png", ".jpg", ".jpeg", ".gif"
    };
}
