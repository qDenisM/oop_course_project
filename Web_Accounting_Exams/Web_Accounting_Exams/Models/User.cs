using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_Accounting_Exams.Models
{
    public enum UserRole
    {
        Admin,
        Applicant
    }

    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        public DateTime? BirthDate { get; set; }

        [Required]
        public UserRole Role { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<ExamApplication> ExamApplications { get; set; } = new List<ExamApplication>();
        public virtual ICollection<UniversityApplication> UniversityApplications { get; set; } = new List<UniversityApplication>();
        public virtual ICollection<ExamResult> ExamResults { get; set; } = new List<ExamResult>();

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
}

