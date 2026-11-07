namespace SchoolManagement.Domain.Entities;

/// <summary>
/// A staff member who can log in (the principal or a teacher).
/// </summary>
public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>Hashed with PBKDF2 (ASP.NET Core PasswordHasher). The plain password is never stored.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public string? Phone { get; set; }
    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public Role Role { get; set; } = null!;
    public NotificationSettings? NotificationSettings { get; set; }
    public ICollection<SchoolClass> HomeroomClasses { get; set; } = new List<SchoolClass>();
    public ICollection<TeacherAssignment> TeacherAssignments { get; set; } = new List<TeacherAssignment>();
    public ICollection<TodoTask> TodoTasks { get; set; } = new List<TodoTask>();
    public ICollection<UploadedFile> UploadedFiles { get; set; } = new List<UploadedFile>();

    public string FullName => $"{FirstName} {LastName}";
}
