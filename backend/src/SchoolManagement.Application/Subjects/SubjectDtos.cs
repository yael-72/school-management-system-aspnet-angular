using System.ComponentModel.DataAnnotations;

namespace SchoolManagement.Application.Subjects;

/// <summary>A subject, as used by filters and forms.</summary>
public record SubjectDto(int Id, string Name);

/// <summary>Body of POST /api/subjects and PUT /api/subjects/{id}.</summary>
public record SaveSubjectRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(50, ErrorMessage = "Name can be at most 50 characters.")]
    public string Name { get; init; } = string.Empty;
}
