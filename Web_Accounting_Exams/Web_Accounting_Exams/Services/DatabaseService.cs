using Microsoft.EntityFrameworkCore;
using Web_Accounting_Exams.Data;
using Web_Accounting_Exams.Models;

namespace Web_Accounting_Exams.Services
{
    public class DatabaseService
    {
        private readonly ApplicationDbContext _context;

        public DatabaseService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> EnsureDatabaseCreatedAsync()
        {
            try
            {
                await _context.Database.EnsureCreatedAsync();
                await UpdateDatabaseSchemaAsync();
                await SeedInitialDataAsync();
                System.Diagnostics.Debug.WriteLine("База данных успешно инициализирована. Пользователи созданы.");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка создания базы данных: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        private async Task UpdateDatabaseSchemaAsync()
        {
            try
            {
                var updateCertificatesSql = @"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Certificates]') AND name = 'Status')
                        ALTER TABLE [Certificates] ADD [Status] INT NOT NULL DEFAULT 0;
                    
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Certificates]') AND name = 'RejectionReason')
                        ALTER TABLE [Certificates] ADD [RejectionReason] NVARCHAR(500) NULL;
                    
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Certificates]') AND name = 'ReviewedAt')
                        ALTER TABLE [Certificates] ADD [ReviewedAt] DATETIME2 NULL;
                    
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Certificates]') AND name = 'ReviewedByUserId')
                        ALTER TABLE [Certificates] ADD [ReviewedByUserId] INT NULL;
                    
                    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Certificates_Users_ReviewedBy')
                        ALTER TABLE [Certificates] ADD CONSTRAINT [FK_Certificates_Users_ReviewedBy] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [Users]([Id]);
                ";

                await _context.Database.ExecuteSqlRawAsync(updateCertificatesSql);

                var updateUniversityAppsSql = @"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UniversityApplications]') AND name = 'Type')
                        ALTER TABLE [UniversityApplications] ADD [Type] INT NOT NULL DEFAULT 0;
                    
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UniversityApplications]') AND name = 'RejectionReason')
                        ALTER TABLE [UniversityApplications] ADD [RejectionReason] NVARCHAR(500) NULL;
                    
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UniversityApplications]') AND name = 'ReviewedAt')
                        ALTER TABLE [UniversityApplications] ADD [ReviewedAt] DATETIME2 NULL;
                    
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[UniversityApplications]') AND name = 'ReviewedByUserId')
                        ALTER TABLE [UniversityApplications] ADD [ReviewedByUserId] INT NULL;
                    
                    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_UniversityApplications_Users_ReviewedBy')
                        ALTER TABLE [UniversityApplications] ADD CONSTRAINT [FK_UniversityApplications_Users_ReviewedBy] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [Users]([Id]);
                ";

                await _context.Database.ExecuteSqlRawAsync(updateUniversityAppsSql);

                var updateSpecialitiesSql = @"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Specialities]') AND name = 'PassingScoreBudget')
                        ALTER TABLE [Specialities] ADD [PassingScoreBudget] INT NULL;
                    
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Specialities]') AND name = 'PassingScorePaid')
                        ALTER TABLE [Specialities] ADD [PassingScorePaid] INT NULL;
                ";

                await _context.Database.ExecuteSqlRawAsync(updateSpecialitiesSql);

                var createPasswordResetTokensSql = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PasswordResetTokens]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [dbo].[PasswordResetTokens] (
                            [Id] INT IDENTITY(1,1) NOT NULL,
                            [UserId] INT NOT NULL,
                            [Code] NVARCHAR(6) NOT NULL,
                            [CreatedAt] DATETIME2 NOT NULL,
                            [ExpiresAt] DATETIME2 NOT NULL,
                            [IsUsed] BIT NOT NULL DEFAULT 0,
                            CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY ([Id]),
                            CONSTRAINT [FK_PasswordResetTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE
                        );
                        
                        CREATE INDEX [IX_PasswordResetTokens_UserId] ON [dbo].[PasswordResetTokens]([UserId]);
                        CREATE INDEX [IX_PasswordResetTokens_Code] ON [dbo].[PasswordResetTokens]([Code]);
                    END
                ";

                await _context.Database.ExecuteSqlRawAsync(createPasswordResetTokensSql);
                
                System.Diagnostics.Debug.WriteLine("Схема базы данных успешно обновлена.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Предупреждение при обновлении схемы БД: {ex.Message}");
            }
        }

        private async Task SeedInitialDataAsync()
        {
            var adminExists = await _context.Users.AnyAsync(u => u.Email == "admin@test.com");
            if (!adminExists)
            {
                var admin = new Models.User
                {
                    Email = "admin@test.com",
                    PasswordHash = HashPassword("admin"),
                    FirstName = "Администратор",
                    LastName = "Системы",
                    Role = Models.UserRole.Admin,
                    Phone = "+375223334455",
                    CreatedAt = DateTime.Now
                };
                _context.Users.Add(admin);
                System.Diagnostics.Debug.WriteLine("Администратор создан: admin@test.com / admin");
            }

            var applicantExists = await _context.Users.AnyAsync(u => u.Email == "student@test.com");
            if (!applicantExists)
            {
                var applicant = new Models.User
                {
                    Email = "student@test.com",
                    PasswordHash = HashPassword("student"),
                    FirstName = "Алексей",
                    LastName = "Петров",
                    Role = Models.UserRole.Applicant,
                    Phone = "+375223334455",
                    BirthDate = new DateTime(2005, 5, 15),
                    CreatedAt = DateTime.Now
                };
                _context.Users.Add(applicant);
                System.Diagnostics.Debug.WriteLine("Абитуриент создан: student@test.com / student123");
            }

            await _context.SaveChangesAsync();

            var examsExist = await _context.Exams.AnyAsync();
            if (!examsExist)
            {
                var exam1 = new Exam
                {
                    Name = "Математика",
                    Subject = "Математика",
                    Description = "Экзамен по математике",
                    DurationMinutes = 180,
                    MaxScore = 100,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                var exam2 = new Exam
                {
                    Name = "Физика",
                    Subject = "Физика",
                    Description = "Экзамен по физике",
                    DurationMinutes = 180,
                    MaxScore = 100,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.Exams.AddRange(exam1, exam2);
                await _context.SaveChangesAsync();

                var schedule1 = new ExamSchedule
                {
                    ExamId = exam1.Id,
                    ExamDate = DateTime.Now.AddDays(30),
                    StartTime = new TimeSpan(10, 0, 0),
                    Location = "Аудитория 101",
                    MaxParticipants = 50,
                    CurrentParticipants = 0,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                var schedule2 = new ExamSchedule
                {
                    ExamId = exam2.Id,
                    ExamDate = DateTime.Now.AddDays(35),
                    StartTime = new TimeSpan(10, 0, 0),
                    Location = "Аудитория 102",
                    MaxParticipants = 50,
                    CurrentParticipants = 0,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.ExamSchedules.AddRange(schedule1, schedule2);
                await _context.SaveChangesAsync();
            }

            var universityExists = await _context.Universities.AnyAsync();
            if (!universityExists)
            {
                var university = new University
                {
                    Name = "Белорусский государственный университет",
                    Address = "г. Минск, пр. Независимости, 4",
                    Phone = "+375 (17) 209-50-00",
                    Email = "info@bsu.by",
                    Website = "https://www.bsu.by",
                    Description = "Ведущий университет Беларуси",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.Universities.Add(university);
                await _context.SaveChangesAsync();

                var speciality = new Speciality
                {
                    UniversityId = university.Id,
                    Name = "Прикладная математика и информатика",
                    Code = "01.03.02",
                    Description = "Подготовка специалистов в области математики и информатики",
                    BudgetPlaces = 25,
                    PaidPlaces = 15,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.Specialities.Add(speciality);
                await _context.SaveChangesAsync();

                var subject1 = new SpecialitySubject
                {
                    SpecialityId = speciality.Id,
                    SubjectName = "Математика",
                    MinScore = 70,
                    IsRequired = true
                };

                var subject2 = new SpecialitySubject
                {
                    SpecialityId = speciality.Id,
                    SubjectName = "Физика",
                    MinScore = 65,
                    IsRequired = true
                };

                _context.SpecialitySubjects.AddRange(subject1, subject2);
                await _context.SaveChangesAsync();
            }
        }

        private string HashPassword(string password)
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
        }

        public ApplicationDbContext GetContext()
        {
            return _context;
        }
    }
}

