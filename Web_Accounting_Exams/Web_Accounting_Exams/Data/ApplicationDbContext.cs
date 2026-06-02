using Microsoft.EntityFrameworkCore;
using Web_Accounting_Exams.Models;

namespace Web_Accounting_Exams.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamSchedule> ExamSchedules { get; set; }
        public DbSet<University> Universities { get; set; }
        public DbSet<Speciality> Specialities { get; set; }
        public DbSet<SpecialitySubject> SpecialitySubjects { get; set; }
        public DbSet<ExamApplication> ExamApplications { get; set; }
        public DbSet<ExamResult> ExamResults { get; set; }
        public DbSet<UniversityApplication> UniversityApplications { get; set; }
        public DbSet<Certificate> Certificates { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(
                    "Server=;Database=;User Id=;Password=;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
            modelBuilder.Entity<ExamSchedule>()
                .HasOne(e => e.Exam)
                .WithMany(e => e.Schedules)
                .HasForeignKey(e => e.ExamId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Speciality>()
                .HasOne(s => s.University)
                .WithMany(u => u.Specialities)
                .HasForeignKey(s => s.UniversityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SpecialitySubject>()
                .HasOne(s => s.Speciality)
                .WithMany(s => s.Subjects)
                .HasForeignKey(s => s.SpecialityId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExamApplication>()
                .HasOne(a => a.User)
                .WithMany(u => u.ExamApplications)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamApplication>()
                .HasOne(a => a.Exam)
                .WithMany(e => e.Applications)
                .HasForeignKey(a => a.ExamId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UniversityApplication>()
                .HasOne(a => a.User)
                .WithMany(u => u.UniversityApplications)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Certificate>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Certificate>()
                .HasOne(c => c.ReviewedBy)
                .WithMany()
                .HasForeignKey(c => c.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UniversityApplication>()
                .HasOne(a => a.ReviewedBy)
                .WithMany()
                .HasForeignKey(a => a.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PasswordResetToken>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

