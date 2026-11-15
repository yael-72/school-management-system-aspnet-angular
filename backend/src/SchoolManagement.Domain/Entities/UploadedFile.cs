namespace SchoolManagement.Domain.Entities;

/// <summary>
/// Metadata of a file a user uploaded. FileName is the name the user sees; StoredPath is generated
/// by the server, so user input never decides where a file is written on disk.
/// </summary>
public class UploadedFile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }

    public User User { get; set; } = null!;
}
