using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolManagement.Application.Assessments;
using SchoolManagement.Application.Common.Exceptions;
using SchoolManagement.Application.Common.Interfaces;
using SchoolManagement.Application.Common.Security;
using SchoolManagement.Domain.Entities;
using SchoolManagement.Domain.Enums;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.UnitTests.TestHelpers;

namespace SchoolManagement.UnitTests.Assessments;

public class AssessmentServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private readonly SchoolBuilder _school;

    public AssessmentServiceTests() => _school = new SchoolBuilder(_db);

    private AssessmentService CreateService(ICurrentUser user) =>
        new(_db, new ClassAccessService(_db, user), user, new FixedTimeProvider(Now), NullLogger<AssessmentService>.Instance);

    private static SaveAssessmentRequest NewAssessment(SchoolClass schoolClass, Subject subject, decimal maxScore = 100) => new()
    {
        Title = "  מבחן בשברים  ",
        Type = AssessmentType.Exam,
        ClassId = schoolClass.Id,
        SubjectId = subject.Id,
        Date = Today,
        MaxScore = maxScore
    };

    private static SaveScoresRequest Scores(params (Student Student, decimal? Score)[] scores) => new()
    {
        Scores = scores.Select(s => new SaveScoreEntry { StudentId = s.Student.Id, Score = s.Score }).ToList()
    };

    /// <summary>A class with a teacher who teaches math in it, and an assessment of that teacher.</summary>
    private async Task<(User Teacher, SchoolClass Class, Subject Math, Assessment Assessment)> MathClassAsync(decimal maxScore = 100)
    {
        var teacher = await _school.AddTeacherAsync();
        var schoolClass = await _school.AddClassAsync();
        var math = await _school.AddSubjectAsync("מתמטיקה");
        await _school.AssignAsync(teacher, schoolClass, math);
        var assessment = await _school.AddAssessmentAsync(schoolClass, math, teacher, maxScore);
        return (teacher, schoolClass, math, assessment);
    }

    // ---------- permissions ----------

    [Fact]
    public async Task Teacher_creates_an_assessment_for_a_subject_she_teaches()
    {
        var (teacher, schoolClass, math, _) = await MathClassAsync();

        var created = await CreateService(FakeCurrentUser.Teacher(teacher.Id))
            .CreateAssessmentAsync(NewAssessment(schoolClass, math));

        created.Title.Should().Be("מבחן בשברים");
        created.CanEdit.Should().BeTrue();
        (await _db.Assessments.SingleAsync(a => a.Id == created.Id)).CreatedByUserId.Should().Be(teacher.Id);
    }

    [Fact]
    public async Task Homeroom_teacher_cannot_create_an_assessment_for_a_subject_she_does_not_teach()
    {
        var homeroomTeacher = await _school.AddTeacherAsync();
        var schoolClass = await _school.AddClassAsync(homeroomTeacher: homeroomTeacher);
        var english = await _school.AddSubjectAsync("אנגלית");

        var act = () => CreateService(FakeCurrentUser.Teacher(homeroomTeacher.Id))
            .CreateAssessmentAsync(NewAssessment(schoolClass, english));

        await act.Should().ThrowAsync<ForbiddenException>();
        (await _db.Assessments.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Homeroom_teacher_sees_all_assessments_of_her_class_but_edits_only_her_subjects()
    {
        var (mathTeacher, schoolClass, math, mathExam) = await MathClassAsync();
        var homeroomTeacher = await _school.AddTeacherAsync("דוד הירש");
        schoolClass.HomeroomTeacherId = homeroomTeacher.Id;
        var english = await _school.AddSubjectAsync("אנגלית");
        await _school.AssignAsync(homeroomTeacher, schoolClass, english);
        var englishExam = await _school.AddAssessmentAsync(schoolClass, english, homeroomTeacher);

        var page = await CreateService(FakeCurrentUser.Teacher(homeroomTeacher.Id))
            .GetAssessmentsAsync(new AssessmentListQuery());

        page.Items.Should().HaveCount(2);
        page.Items.Single(a => a.Id == englishExam.Id).CanEdit.Should().BeTrue();
        page.Items.Single(a => a.Id == mathExam.Id).CanEdit.Should().BeFalse();
    }

    [Fact]
    public async Task Teacher_does_not_see_assessments_of_classes_she_does_not_teach()
    {
        var (teacher, _, math, _) = await MathClassAsync();
        var otherClass = await _school.AddClassAsync("ח'1", 8);
        var colleague = await _school.AddTeacherAsync("דוד הירש");
        await _school.AddAssessmentAsync(otherClass, math, colleague);
        var service = CreateService(FakeCurrentUser.Teacher(teacher.Id));

        var page = await service.GetAssessmentsAsync(new AssessmentListQuery());

        page.Items.Should().ContainSingle().Which.ClassName.Should().Be("ז'1");
        await service.Invoking(s => s.GetAssessmentsAsync(new AssessmentListQuery { ClassId = otherClass.Id }))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Teacher_cannot_grade_an_assessment_of_another_teachers_subject()
    {
        var (_, schoolClass, _, mathExam) = await MathClassAsync();
        var homeroomTeacher = await _school.AddTeacherAsync("דוד הירש");
        schoolClass.HomeroomTeacherId = homeroomTeacher.Id;
        await _db.SaveChangesAsync();
        var student = await _school.AddStudentAsync(schoolClass);

        var service = CreateService(FakeCurrentUser.Teacher(homeroomTeacher.Id));

        (await service.GetScoresAsync(mathExam.Id)).Assessment.CanEdit.Should().BeFalse();
        await service.Invoking(s => s.SaveScoresAsync(mathExam.Id, Scores((student, 90))))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Missing_assessment_is_not_found()
    {
        var act = () => CreateService(FakeCurrentUser.Admin()).GetScoresAsync(999);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---------- score rules ----------

    [Fact]
    public async Task Saving_scores_creates_one_score_per_student_and_returns_the_grade_sheet()
    {
        var (teacher, schoolClass, _, assessment) = await MathClassAsync(maxScore: 20);
        var yosef = await _school.AddStudentAsync(schoolClass, "יוסף", "אברהם");
        var david = await _school.AddStudentAsync(schoolClass, "דוד", "ברק");
        await _school.AddStudentAsync(schoolClass, "משה", "גל");

        var sheet = await CreateService(FakeCurrentUser.Teacher(teacher.Id))
            .SaveScoresAsync(assessment.Id, Scores((yosef, 18), (david, 15)));

        sheet.Students.Select(s => (s.FirstName, s.Score, s.Percent)).Should().Equal(
            ("יוסף", 18m, 90m),
            ("דוד", 15m, 75m),
            ("משה", (decimal?)null, (decimal?)null));
        sheet.GradedCount.Should().Be(2);
        sheet.AverageScore.Should().Be(82.5m);
        sheet.HighestScore.Should().Be(18);
        sheet.LowestScore.Should().Be(15);
        (await _db.Scores.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Saving_again_updates_instead_of_duplicating()
    {
        var (_, schoolClass, _, assessment) = await MathClassAsync();
        var student = await _school.AddStudentAsync(schoolClass);
        var service = CreateService(FakeCurrentUser.Admin());

        await service.SaveScoresAsync(assessment.Id, Scores((student, 70)));
        await service.SaveScoresAsync(assessment.Id, Scores((student, 85.5m)));

        (await _db.Scores.SingleAsync()).Value.Should().Be(85.5m);
    }

    [Fact]
    public async Task An_empty_row_clears_the_students_score()
    {
        var (_, schoolClass, _, assessment) = await MathClassAsync();
        var student = await _school.AddStudentAsync(schoolClass);
        await _school.AddScoreAsync(assessment, student, 70);

        await CreateService(FakeCurrentUser.Admin()).SaveScoresAsync(assessment.Id, Scores((student, null)));

        (await _db.Scores.AnyAsync()).Should().BeFalse();
    }

    [Theory]
    [InlineData(101)]
    [InlineData(-1)]
    public async Task Score_must_be_between_zero_and_the_max_score(decimal score)
    {
        var (_, schoolClass, _, assessment) = await MathClassAsync(maxScore: 100);
        var student = await _school.AddStudentAsync(schoolClass);

        var act = () => CreateService(FakeCurrentUser.Admin()).SaveScoresAsync(assessment.Id, Scores((student, score)));

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*between 0 and the max score (100)*");
        (await _db.Scores.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Scores_cannot_be_entered_before_the_assessment_date()
    {
        var (teacher, schoolClass, math, _) = await MathClassAsync();
        var upcoming = await _school.AddAssessmentAsync(schoolClass, math, teacher, date: Today.AddDays(3));
        var student = await _school.AddStudentAsync(schoolClass);

        var act = () => CreateService(FakeCurrentUser.Admin()).SaveScoresAsync(upcoming.Id, Scores((student, 90)));

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*before the assessment date*");
    }

    [Fact]
    public async Task Students_who_are_not_active_in_the_class_are_rejected()
    {
        var (_, schoolClass, _, assessment) = await MathClassAsync();
        var inactive = await _school.AddStudentAsync(schoolClass, isActive: false);
        var stranger = await _school.AddStudentAsync(await _school.AddClassAsync("ח'1", 8));

        var act = () => CreateService(FakeCurrentUser.Admin())
            .SaveScoresAsync(assessment.Id, Scores((inactive, 80), (stranger, 90)));

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*not active students of this class*");
    }

    [Fact]
    public async Task The_same_student_cannot_appear_twice()
    {
        var (_, schoolClass, _, assessment) = await MathClassAsync();
        var student = await _school.AddStudentAsync(schoolClass);

        var act = () => CreateService(FakeCurrentUser.Admin())
            .SaveScoresAsync(assessment.Id, Scores((student, 80), (student, 90)));

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*only once*");
    }

    [Fact]
    public async Task Inactive_students_are_left_out_of_the_grade_sheet_and_the_averages()
    {
        var (_, schoolClass, _, assessment) = await MathClassAsync();
        var active = await _school.AddStudentAsync(schoolClass, "יוסף");
        var inactive = await _school.AddStudentAsync(schoolClass, "דוד", isActive: false);
        await _school.AddScoreAsync(assessment, active, 80);
        await _school.AddScoreAsync(assessment, inactive, 20);
        var service = CreateService(FakeCurrentUser.Admin());

        var sheet = await service.GetScoresAsync(assessment.Id);
        var row = (await service.GetAssessmentsAsync(new AssessmentListQuery())).Items.Single();

        sheet.Students.Should().ContainSingle().Which.FirstName.Should().Be("יוסף");
        sheet.AverageScore.Should().Be(80);
        row.AverageScore.Should().Be(80);
        row.GradedCount.Should().Be(1);
        row.StudentCount.Should().Be(1);
    }

    // ---------- editing ----------

    [Fact]
    public async Task Max_score_cannot_be_lowered_below_an_existing_score()
    {
        var (_, schoolClass, math, assessment) = await MathClassAsync();
        await _school.AddScoreAsync(assessment, await _school.AddStudentAsync(schoolClass), 95);

        var act = () => CreateService(FakeCurrentUser.Admin())
            .UpdateAssessmentAsync(assessment.Id, NewAssessment(schoolClass, math, maxScore: 90));

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*lower than an existing score (95)*");
    }

    [Fact]
    public async Task An_assessment_with_scores_cannot_move_to_another_class()
    {
        var (_, schoolClass, math, assessment) = await MathClassAsync();
        await _school.AddScoreAsync(assessment, await _school.AddStudentAsync(schoolClass), 80);
        var otherClass = await _school.AddClassAsync("ח'1", 8);

        var act = () => CreateService(FakeCurrentUser.Admin())
            .UpdateAssessmentAsync(assessment.Id, NewAssessment(otherClass, math));

        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*cannot be moved to another class*");
    }

    [Fact]
    public async Task Deleting_an_assessment_deletes_its_scores()
    {
        var (teacher, schoolClass, _, assessment) = await MathClassAsync();
        await _school.AddScoreAsync(assessment, await _school.AddStudentAsync(schoolClass), 80);

        await CreateService(FakeCurrentUser.Teacher(teacher.Id)).DeleteAssessmentAsync(assessment.Id);

        (await _db.Assessments.AnyAsync()).Should().BeFalse();
        (await _db.Scores.AnyAsync()).Should().BeFalse();
    }

    // ---------- list ----------

    [Fact]
    public async Task List_filters_by_subject_and_date_range_and_shows_the_newest_first()
    {
        var (teacher, schoolClass, math, _) = await MathClassAsync(); // dated 2026-10-01
        var english = await _school.AddSubjectAsync("אנגלית");
        await _school.AddAssessmentAsync(schoolClass, math, teacher, date: new DateOnly(2026, 10, 20), title: "בוחן");
        await _school.AddAssessmentAsync(schoolClass, english, teacher, date: new DateOnly(2026, 10, 15));
        await _school.AddAssessmentAsync(schoolClass, math, teacher, date: new DateOnly(2026, 9, 1));

        var page = await CreateService(FakeCurrentUser.Admin()).GetAssessmentsAsync(new AssessmentListQuery
        {
            SubjectId = math.Id,
            From = new DateOnly(2026, 9, 15),
            To = new DateOnly(2026, 10, 31)
        });

        page.Items.Select(a => a.Date).Should().Equal(new DateOnly(2026, 10, 20), new DateOnly(2026, 10, 1));
    }
}
