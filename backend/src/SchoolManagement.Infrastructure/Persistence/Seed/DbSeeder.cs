using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Domain.Constants;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;

namespace SchoolManagement.Infrastructure.Persistence.Seed;

/// <summary>
/// Fills an empty database with realistic demo data so the dashboard looks alive on first run.
///
/// Why a runtime seeder and not EF Core HasData: the demo has thousands of score and attendance
/// rows, and their dates are relative to "today". HasData would put all of that, with fixed dates,
/// into a migration file. Only fixed reference data (roles) uses HasData.
///
/// The random generator uses a fixed seed, so every run produces the same names and grades.
/// </summary>
public class DbSeeder(
    AppDbContext context,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider,
    ILogger<DbSeeder> logger)
{
    /// <summary>Password of every demo account. Documented in the README; development data only.</summary>
    public const string DemoPassword = "Demo123!";
    public const string AdminEmail = "admin@school.demo";
    public const string TeacherEmail = "teacher@school.demo";

    private const int StudentsPerClass = 25;
    private const int AttendanceHistoryDays = 60;
    private const int AssessmentHistoryDays = 120;

    private readonly Random _random = new(2026);
    private DateTime _now;
    private DateOnly _today;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await context.Users.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Database already contains data, skipping demo seed");
            return;
        }

        _now = timeProvider.GetUtcNow().UtcDateTime;
        _today = DateOnly.FromDateTime(_now);

        logger.LogInformation("Seeding demo data...");

        var (admin, teachers) = CreateStaff();
        var subjects = CreateSubjects();
        var classes = CreateClasses(teachers);
        var assignments = CreateTeacherAssignments(classes, subjects, teachers);
        await context.SaveChangesAsync(cancellationToken);

        var students = CreateStudents(classes);
        await context.SaveChangesAsync(cancellationToken);

        CreateAssessmentsAndScores(assignments, students);
        await context.SaveChangesAsync(cancellationToken);

        CreateAttendance(students, teachers);
        await context.SaveChangesAsync(cancellationToken);

        CreateEvents(classes, admin);
        CreateTodoTasks(admin, teachers[0]);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Demo data seeded: {Teachers} teachers, {Classes} classes, {Students} students",
            teachers.Count, classes.Count, students.Count);
    }

    private (User Admin, List<User> Teachers) CreateStaff()
    {
        var admin = CreateUser("אהרן אברמסון", AdminEmail, RoleIds.Admin);

        string[] teacherNames =
            ["משה לוינסון", "דוד הירש", "אברהם גרינפלד", "יוסף ברקוביץ", "חיים רוזנטל", "שמואל אורנשטיין"];

        // The first teacher gets the easy-to-remember demo email.
        var teachers = teacherNames
            .Select((name, i) => CreateUser(
                name, i == 0 ? TeacherEmail : $"teacher{i + 1}@school.demo", RoleIds.Teacher))
            .ToList();

        context.Users.Add(admin);
        context.Users.AddRange(teachers);
        return (admin, teachers);
    }

    private User CreateUser(string fullName, string email, int roleId)
    {
        var user = new User
        {
            FullName = fullName,
            Email = email,
            Phone = RandomPhone(),
            RoleId = roleId,
            IsActive = true,
            CreatedAt = _now.AddDays(-365),
            NotificationSettings = new NotificationSettings()
        };
        user.PasswordHash = passwordHasher.Hash(DemoPassword);
        return user;
    }

    private List<Subject> CreateSubjects()
    {
        string[] names = ["מתמטיקה", "אנגלית", "לשון", "תנ\"ך", "היסטוריה", "מדעים"];
        var subjects = names.Select(name => new Subject { Name = name }).ToList();
        context.Subjects.AddRange(subjects);
        return subjects;
    }

    private List<SchoolClass> CreateClasses(List<User> teachers)
    {
        (string Name, byte Grade)[] definitions =
        [
            ("ז'1", 7), ("ז'2", 7), ("ח'1", 8), ("ט'1", 9),
            ("י'1", 10), ("י\"א1", 11), ("י\"א2", 11), ("י\"ב1", 12)
        ];

        var schoolYear = CurrentSchoolYear();
        var classes = definitions
            .Select((d, i) => new SchoolClass
            {
                Name = d.Name,
                GradeLevel = d.Grade,
                SchoolYear = schoolYear,
                HomeroomTeacher = teachers[i % teachers.Count]
            })
            .ToList();

        context.Classes.AddRange(classes);
        return classes;
    }

    /// <summary>
    /// Each teacher teaches two subjects: one in the younger classes (grades 7–9) and another in
    /// the older classes (10–12). This gives every teacher a realistic, limited set of classes,
    /// which makes the "teachers only see their own classes" rule visible in the demo.
    /// </summary>
    private List<TeacherAssignment> CreateTeacherAssignments(List<SchoolClass> classes, List<Subject> subjects, List<User> teachers)
    {
        var assignments = new List<TeacherAssignment>();
        foreach (var schoolClass in classes)
        {
            var offset = schoolClass.GradeLevel <= 9 ? 0 : teachers.Count / 2;
            for (var s = 0; s < subjects.Count; s++)
            {
                assignments.Add(new TeacherAssignment
                {
                    Class = schoolClass,
                    Subject = subjects[s],
                    Teacher = teachers[(s + offset) % teachers.Count]
                });
            }
        }

        context.TeacherAssignments.AddRange(assignments);
        return assignments;
    }

    private List<Student> CreateStudents(List<SchoolClass> classes)
    {
        var students = new List<Student>();
        var schoolYearStart = SchoolYearStartYear();

        foreach (var schoolClass in classes)
        {
            for (var i = 0; i < StudentsPerClass; i++)
            {
                var firstName = Pick(DemoNames.FirstNames);
                var lastName = Pick(DemoNames.LastNames);

                // A 7th grader is about 12 years old at the start of the school year.
                var birthYear = schoolYearStart - (schoolClass.GradeLevel + 5);
                var birthDate = new DateOnly(birthYear, 1, 1).AddDays(_random.Next(365));

                students.Add(new Student
                {
                    FirstName = firstName,
                    LastName = lastName,
                    BirthDate = birthDate,
                    ParentPhone = RandomPhone(),
                    Address = $"{Pick(DemoNames.Streets)} {_random.Next(1, 60)}, {Pick(DemoNames.Cities)}",
                    Class = schoolClass,
                    IsActive = true
                });
            }
        }

        // A few students who left the school, to demonstrate soft delete.
        foreach (var student in students.Where((_, i) => i % 67 == 66))
        {
            student.IsActive = false;
        }

        context.Students.AddRange(students);
        return students;
    }

    /// <summary>
    /// Creates assessments for every class + subject (one per teacher assignment), created by the
    /// teacher who teaches that subject in that class.
    /// </summary>
    private void CreateAssessmentsAndScores(List<TeacherAssignment> assignments, List<Student> students)
    {
        // Each student gets a stable "ability" so their grades are consistent across subjects,
        // which makes the top-students list and grade distribution look realistic.
        var ability = students.ToDictionary(s => s, _ => Math.Clamp(NextGaussian(80, 9), 55, 98));
        var studentsByClass = students.Where(s => s.IsActive).ToLookup(s => s.Class);

        foreach (var (schoolClass, subject, teacher) in assignments.Select(a => (a.Class, a.Subject, a.Teacher)))
        {
            // Spread the first assessment of each class+subject so not everything falls on the same day.
            var date = _today.AddDays(-AssessmentHistoryDays + _random.Next(0, 14));
            var number = 1;

            // Past assessments with scores, plus a few upcoming ones (next ~2 weeks) without scores.
            while (date <= _today.AddDays(14))
            {
                date = NextSchoolDay(date);
                var isQuiz = number % 3 == 0;
                var assessment = new Assessment
                {
                    Title = isQuiz ? $"בוחן {subject.Name} {number}" : $"מבחן {subject.Name} {number}",
                    Type = isQuiz ? AssessmentType.Quiz : AssessmentType.Exam,
                    Class = schoolClass,
                    Subject = subject,
                    Date = date,
                    MaxScore = isQuiz ? 20 : 100,
                    CreatedByUser = teacher,
                    CreatedAt = date.AddDays(-7).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                };
                context.Assessments.Add(assessment);

                if (date < _today)
                {
                    foreach (var student in studentsByClass[schoolClass])
                    {
                        var percent = Math.Clamp(NextGaussian(ability[student], 7), 35, 100);

                        // About 2% of students missed the assessment and have no score yet (null).
                        var missed = _random.NextDouble() < 0.02;

                        context.Scores.Add(new Score
                        {
                            Assessment = assessment,
                            Student = student,
                            Value = missed ? null : Math.Round((decimal)percent * assessment.MaxScore / 100m),
                            UpdatedAt = date.AddDays(3).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                        });
                    }
                }

                date = date.AddDays(_random.Next(14, 25));
                number++;
            }
        }
    }

    private void CreateAttendance(List<Student> students, List<User> teachers)
    {
        // A handful of students miss school often, so the dashboard's "frequent absences" list has content.
        var activeStudents = students.Where(s => s.IsActive).ToList();
        var frequentAbsentees = activeStudents.Where((_, i) => i % 23 == 5).ToHashSet();

        for (var date = _today.AddDays(-AttendanceHistoryDays); date <= _today; date = date.AddDays(1))
        {
            if (!IsSchoolDay(date))
            {
                continue;
            }

            foreach (var student in activeStudents)
            {
                var absentChance = frequentAbsentees.Contains(student) ? 0.22 : 0.04;
                var roll = _random.NextDouble();
                var status = roll < absentChance ? AttendanceStatus.Absent
                    : roll < absentChance + 0.03 ? AttendanceStatus.Late
                    : roll < absentChance + 0.05 ? AttendanceStatus.Excused
                    : AttendanceStatus.Present;

                context.Attendance.Add(new AttendanceRecord
                {
                    Student = student,
                    Date = date,
                    Status = status,
                    RecordedByUser = student.Class.HomeroomTeacher ?? teachers[0]
                });
            }
        }
    }

    private void CreateEvents(List<SchoolClass> classes, User admin)
    {
        SchoolEvent Event(string title, int daysFromToday, int durationDays = 1, SchoolClass? schoolClass = null) => new()
        {
            Title = title,
            StartDate = _today.AddDays(daysFromToday),
            EndDate = durationDays > 1 ? _today.AddDays(daysFromToday + durationDays - 1) : null,
            Class = schoolClass,
            CreatedByUser = admin
        };

        context.Events.AddRange(
            Event("ישיבת צוות", -6),
            Event("אסיפת הורים", 2),
            Event("טיול שנתי", 9, durationDays: 2, schoolClass: classes.First(c => c.GradeLevel == 9)),
            Event("מבחן מתכונת במתמטיקה", 12, schoolClass: classes.First(c => c.GradeLevel == 12)),
            Event("ישיבת מחנכים", 16),
            Event("יום פתוח לנרשמים", 23));
    }

    private void CreateTodoTasks(User admin, User teacher)
    {
        TodoTask Todo(User user, string title, int dueInDays, bool done = false) => new()
        {
            User = user,
            Title = title,
            DueDate = _today.AddDays(dueInDays),
            IsDone = done,
            CreatedAt = _now.AddDays(-3)
        };

        context.TodoTasks.AddRange(
            Todo(admin, "לאשר את מערכת המבחנים לסוף המחצית", 3),
            Todo(admin, "להכין סדר יום לאסיפת ההורים", 2),
            Todo(admin, "לבדוק דוח היעדרויות חודשי", -1, done: true),
            Todo(admin, "לשבץ מורה מחליף לשכבת י'", 5),
            Todo(teacher, "לבדוק את מבחני המתמטיקה של ז'1", 1),
            Todo(teacher, "להתקשר להורים של תלמידים שנעדרו", 2),
            Todo(teacher, "להכין דף עבודה לשיעור הבא", 4, done: true));
    }

    // ---------- helpers ----------

    /// <summary>The Israeli school year starts on 1 September.</summary>
    private int SchoolYearStartYear() => _today.Month >= 9 ? _today.Year : _today.Year - 1;

    private string CurrentSchoolYear()
    {
        var start = SchoolYearStartYear();
        return $"{start}-{start + 1}";
    }

    /// <summary>School days are Sunday to Thursday.</summary>
    private static bool IsSchoolDay(DateOnly date) =>
        date.DayOfWeek is not (DayOfWeek.Friday or DayOfWeek.Saturday);

    private static DateOnly NextSchoolDay(DateOnly date)
    {
        while (!IsSchoolDay(date))
        {
            date = date.AddDays(1);
        }
        return date;
    }

    private T Pick<T>(IReadOnlyList<T> items) => items[_random.Next(items.Count)];

    private string RandomPhone() => $"05{_random.Next(0, 9)}-{_random.Next(1000000, 9999999)}";

    /// <summary>Normally distributed random number (Box–Muller transform).</summary>
    private double NextGaussian(double mean, double standardDeviation)
    {
        var u1 = 1.0 - _random.NextDouble();
        var u2 = _random.NextDouble();
        var standardNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + standardDeviation * standardNormal;
    }
}
