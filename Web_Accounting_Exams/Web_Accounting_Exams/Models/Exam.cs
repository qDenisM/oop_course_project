using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_Accounting_Exams.Models
{
    public class Exam
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(50)]
        public string Subject { get; set; } = string.Empty;

        public int DurationMinutes { get; set; }

        public int MaxScore { get; set; } = 100;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<ExamSchedule> Schedules { get; set; } = new List<ExamSchedule>();
        public virtual ICollection<ExamApplication> Applications { get; set; } = new List<ExamApplication>();
        public virtual ICollection<ExamResult> Results { get; set; } = new List<ExamResult>();
    }
}

