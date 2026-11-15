-- School Management System - original schema design (SQL Server).
-- Reference only: the EF Core migrations in backend/src/SchoolManagement.Infrastructure
-- are the source of truth for the real database.
-- Exported from SSMS (Generate Scripts, schema only); machine-specific database file settings removed.

CREATE DATABASE [SchoolManagement]
GO
USE [SchoolManagement]
GO
/****** Object:  Table [dbo].[Assessments]    Script Date: 07/11/2026 23:10:06 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Assessments](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nvarchar](100) NOT NULL,
	[Type] [nvarchar](20) NOT NULL,
	[ClassId] [int] NOT NULL,
	[SubjectId] [int] NOT NULL,
	[Date] [date] NOT NULL,
	[MaxScore] [decimal](5, 2) NOT NULL,
	[CreatedByUserId] [int] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Attendance]    Script Date: 07/11/2026 23:10:06 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Attendance](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[StudentId] [int] NOT NULL,
	[Date] [date] NOT NULL,
	[Status] [nvarchar](10) NOT NULL,
	[Note] [nvarchar](200) NULL,
	[RecordedByUserId] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Attendance] UNIQUE NONCLUSTERED 
(
	[StudentId] ASC,
	[Date] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Classes]    Script Date: 07/11/2026 23:10:06 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Classes](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](20) NOT NULL,
	[GradeLevel] [tinyint] NOT NULL,
	[SchoolYear] [nvarchar](10) NOT NULL,
	[HomeroomTeacherId] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Classes_Name_Year] UNIQUE NONCLUSTERED 
(
	[Name] ASC,
	[SchoolYear] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Events]    Script Date: 07/11/2026 23:10:06 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Events](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nvarchar](100) NOT NULL,
	[StartDate] [date] NOT NULL,
	[EndDate] [date] NULL,
	[ClassId] [int] NULL,
	[CreatedByUserId] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[NotificationSettings]    Script Date: 07/11/2026 23:10:06 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NotificationSettings](
	[UserId] [int] NOT NULL,
	[RemindBeforeExam] [bit] NOT NULL,
	[AlertOnLowAttendance] [bit] NOT NULL,
	[WeeklySummaryEmail] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Roles]    Script Date: 07/11/2026 23:10:06 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Roles](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Role] [nvarchar](30) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Role] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Scores]    Script Date: 07/11/2026 23:10:06 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Scores](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[AssessmentId] [int] NOT NULL,
	[StudentId] [int] NOT NULL,
	[Score] [decimal](5, 2) NULL,
	[Comment] [nvarchar](300) NULL,
	[UpdatedAt] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Score] UNIQUE NONCLUSTERED 
(
	[AssessmentId] ASC,
	[StudentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Students]    Script Date: 07/11/2026 23:10:06 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Students](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FirstName] [nvarchar](50) NOT NULL,
	[LastName] [nvarchar](50) NOT NULL,
	[ClassId] [int] NOT NULL,
	[BirthDate] [date] NULL,
	[ParentPhone] [nvarchar](20) NULL,
	[Address] [nvarchar](200) NULL,
	[IsActive] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Subjects]    Script Date: 07/11/2026 23:10:06 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Subjects](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TeacherAssignments]    Script Date: 07/11/2026 23:10:06 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TeacherAssignments](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TeacherId] [int] NOT NULL,
	[ClassId] [int] NOT NULL,
	[SubjectId] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_TeacherAssignment] UNIQUE NONCLUSTERED 
(
	[TeacherId] ASC,
	[ClassId] ASC,
	[SubjectId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TodoTasks]    Script Date: 07/11/2026 23:10:06 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TodoTasks](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[Title] [nvarchar](200) NOT NULL,
	[DueDate] [date] NULL,
	[IsDone] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UploadedFiles]    Script Date: 07/11/2026 23:10:06 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UploadedFiles](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[FileName] [nvarchar](200) NOT NULL,
	[StoredPath] [nvarchar](400) NOT NULL,
	[SizeBytes] [bigint] NOT NULL,
	[UploadedAt] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Users]    Script Date: 07/11/2026 23:10:06 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Users](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[FullName] [nvarchar](100) NOT NULL,
	[Email] [nvarchar](150) NOT NULL,
	[PasswordHash] [nvarchar](255) NOT NULL,
	[RoleId] [int] NOT NULL,
	[Phone] [nvarchar](20) NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Index [IX_Assessments_Class_Date]    Script Date: 07/11/2026 23:10:06 ******/
CREATE NONCLUSTERED INDEX [IX_Assessments_Class_Date] ON [dbo].[Assessments]
(
	[ClassId] ASC,
	[Date] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Attendance_Date]    Script Date: 07/11/2026 23:10:06 ******/
CREATE NONCLUSTERED INDEX [IX_Attendance_Date] ON [dbo].[Attendance]
(
	[Date] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Scores_StudentId]    Script Date: 07/11/2026 23:10:06 ******/
CREATE NONCLUSTERED INDEX [IX_Scores_StudentId] ON [dbo].[Scores]
(
	[StudentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_Students_ClassId]    Script Date: 07/11/2026 23:10:06 ******/
CREATE NONCLUSTERED INDEX [IX_Students_ClassId] ON [dbo].[Students]
(
	[ClassId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Assessments] ADD  DEFAULT ((100)) FOR [MaxScore]
GO
ALTER TABLE [dbo].[Assessments] ADD  DEFAULT (sysdatetime()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[NotificationSettings] ADD  DEFAULT ((1)) FOR [RemindBeforeExam]
GO
ALTER TABLE [dbo].[NotificationSettings] ADD  DEFAULT ((1)) FOR [AlertOnLowAttendance]
GO
ALTER TABLE [dbo].[NotificationSettings] ADD  DEFAULT ((0)) FOR [WeeklySummaryEmail]
GO
ALTER TABLE [dbo].[Scores] ADD  DEFAULT (sysdatetime()) FOR [UpdatedAt]
GO
ALTER TABLE [dbo].[Students] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[TodoTasks] ADD  DEFAULT ((0)) FOR [IsDone]
GO
ALTER TABLE [dbo].[TodoTasks] ADD  DEFAULT (sysdatetime()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[UploadedFiles] ADD  DEFAULT (sysdatetime()) FOR [UploadedAt]
GO
ALTER TABLE [dbo].[Users] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Users] ADD  DEFAULT (sysdatetime()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Assessments]  WITH CHECK ADD FOREIGN KEY([ClassId])
REFERENCES [dbo].[Classes] ([Id])
GO
ALTER TABLE [dbo].[Assessments]  WITH CHECK ADD FOREIGN KEY([CreatedByUserId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[Assessments]  WITH CHECK ADD FOREIGN KEY([SubjectId])
REFERENCES [dbo].[Subjects] ([Id])
GO
ALTER TABLE [dbo].[Attendance]  WITH CHECK ADD FOREIGN KEY([RecordedByUserId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[Attendance]  WITH CHECK ADD FOREIGN KEY([StudentId])
REFERENCES [dbo].[Students] ([Id])
GO
ALTER TABLE [dbo].[Classes]  WITH CHECK ADD FOREIGN KEY([HomeroomTeacherId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[Events]  WITH CHECK ADD FOREIGN KEY([ClassId])
REFERENCES [dbo].[Classes] ([Id])
GO
ALTER TABLE [dbo].[Events]  WITH CHECK ADD FOREIGN KEY([CreatedByUserId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[NotificationSettings]  WITH CHECK ADD FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[Scores]  WITH CHECK ADD FOREIGN KEY([AssessmentId])
REFERENCES [dbo].[Assessments] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Scores]  WITH CHECK ADD FOREIGN KEY([StudentId])
REFERENCES [dbo].[Students] ([Id])
GO
ALTER TABLE [dbo].[Students]  WITH CHECK ADD FOREIGN KEY([ClassId])
REFERENCES [dbo].[Classes] ([Id])
GO
ALTER TABLE [dbo].[TeacherAssignments]  WITH CHECK ADD FOREIGN KEY([ClassId])
REFERENCES [dbo].[Classes] ([Id])
GO
ALTER TABLE [dbo].[TeacherAssignments]  WITH CHECK ADD FOREIGN KEY([SubjectId])
REFERENCES [dbo].[Subjects] ([Id])
GO
ALTER TABLE [dbo].[TeacherAssignments]  WITH CHECK ADD FOREIGN KEY([TeacherId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[TodoTasks]  WITH CHECK ADD FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[UploadedFiles]  WITH CHECK ADD FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[Users]  WITH CHECK ADD FOREIGN KEY([RoleId])
REFERENCES [dbo].[Roles] ([Id])
GO
ALTER TABLE [dbo].[Assessments]  WITH CHECK ADD CHECK  (([Type]=N'Quiz' OR [Type]=N'Assignment' OR [Type]=N'Exam'))
GO
ALTER TABLE [dbo].[Attendance]  WITH CHECK ADD CHECK  (([Status]=N'Excused' OR [Status]=N'Late' OR [Status]=N'Absent' OR [Status]=N'Present'))
GO
ALTER TABLE [dbo].[Classes]  WITH CHECK ADD CHECK  (([GradeLevel]>=(1) AND [GradeLevel]<=(12)))
GO
