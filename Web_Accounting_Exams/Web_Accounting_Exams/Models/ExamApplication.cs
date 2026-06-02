using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_Accounting_Exams.Models
{
    public enum ApplicationStatus
    {
        Pending,
        Approved,
        Rejected,
        Completed
    }

    public class ExamApplication
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int ExamId { get; set; }

        [Required]
        public int ExamScheduleId { get; set; }

        [Required]
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

        [MaxLength(500)]
        public string? Notes { get; set; }

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        public DateTime AppliedAt { get; set; } = DateTime.Now;

        public DateTime? ReviewedAt { get; set; }

        public int? ReviewedByUserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; } = null!;

        [ForeignKey("ExamScheduleId")]
        public virtual ExamSchedule ExamSchedule { get; set; } = null!;

        [ForeignKey("ReviewedByUserId")]
        public virtual User? ReviewedBy { get; set; }
    }
}

