using System.ComponentModel.DataAnnotations;

namespace SchoolManagement.Infrastructure.Files;

/// <summary>Bound from the "FileStorage" section of appsettings.</summary>
public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>Folder for uploaded files. A relative path is relative to the app's content root.</summary>
    [Required]
    public string RootPath { get; set; } = "App_Data/uploads";
}
