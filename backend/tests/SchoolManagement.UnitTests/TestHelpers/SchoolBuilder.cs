using SchoolManagement.Domain.Constants;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Infrastructure.Persistence;

namespace SchoolManagement.UnitTests.TestHelpers;

/// <summary>
/// Small helpers that add rows to the test database, so each test only shows the data that matters to it.
/// </summary>
public class SchoolBuilder(AppDbContext db)
{
    public async Task<User> AddTeacherAsync(string name = "משה כהן")
    {
        var teacher = new User
        {
            FullName = name,
            Email = $"{Guid.NewGuid():N}@school.test",
            PasswordHash = "not-used",
            RoleId = RoleIds.Teacher,
            IsActive = true
        };
        db.Users.Add(teacher);
        await db.SaveChangesAsync();
        return teacher;
    }

    public async Task<SchoolClass> AddClassAsync(string name = "ז'1", byte gradeLevel = 7, User? homeroomTeacher = null)
    {
        var schoolClass = new SchoolClass
        {
            Name = name,
            GradeLevel = gradeLevel,
            SchoolYear = "2026-2027",
            HomeroomTeacherId = homeroomTeacher?.Id
        };
        db.Classes.Add(schoolClass);
        await db.SaveChangesAsync();
        return schoolClass;
    }

    public async Task<Subject> AddSubjectAsync(string name = "מתמטיקה")
    {
        var subject = new Subject { Name = name };
        db.Subjects.Add(subject);
        await db.SaveChangesAsync();
        return subject;
    }

    public async Task AssignAsync(User teacher, SchoolClass schoolClass, Subject subject)
    {
        db.TeacherAssignments.Add(new TeacherAssignment
        {
            TeacherId = teacher.Id,
            ClassId = schoolClass.Id,
            SubjectId = subject.Id
        });
        await db.SaveChangesAsync();
    }

    public async Task<Student> AddStudentAsync(SchoolClass schoolClass, string firstName = "יוסף", string lastName = "לוי", bool isActive = true)
    {
        var student = new Student
        {
            FirstName = firstName,
            LastName = lastName,
            ClassId = schoolClass.Id,
            IsActive = isActive
        };
        db.Students.Add(student);
        await db.SaveChangesAsync();
        return student;
    }

    public async Task<Assessment> AddAssessmentAsync(SchoolClass schoolClass, Subject subject, User createdBy,
        decimal maxScore = 100, DateOnly? date = null, string title = "מבחן")
    {
        var assessment = new Assessment
        {
            Title = title,
            Type = AssessmentType.Exam,
            ClassId = schoolClass.Id,
            SubjectId = subject.Id,
            CreatedByUserId = createdBy.Id,
            Date = date ?? new DateOnly(2026, 10, 1),
            MaxScore = maxScore
        };
        db.Assessments.Add(assessment);
        await db.SaveChangesAsync();
        return assessment;
    }

    public async Task AddScoreAsync(Assessment assessment, Student student, decimal? value)
    {
        db.Scores.Add(new Score { AssessmentId = assessment.Id, StudentId = student.Id, Value = value });
        await db.SaveChangesAsync();
    }

    public async Task AddAttendanceAsync(Student student, User recordedBy, params AttendanceStatus[] statuses)
    {
        var date = new DateOnly(2026, 10, 1);
        foreach (var status in statuses)
        {
            db.Attendance.Add(new AttendanceRecord
            {
                StudentId = student.Id,
                Date = date,
                Status = status,
                RecordedByUserId = recordedBy.Id
            });
            date = date.AddDays(1);
        }
        await db.SaveChangesAsync();
    }
}
