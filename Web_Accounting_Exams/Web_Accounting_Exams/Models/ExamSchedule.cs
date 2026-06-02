using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_Accounting_Exams.Models
{
    public class ExamSchedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ExamId { get; set; }

        [Required]
        public DateTime ExamDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        public int MaxParticipants { get; set; }

        public int CurrentParticipants { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; } = null!;

        [NotMapped]
        public string DisplayText => $"{ExamDate:dd.MM.yyyy} {StartTime:hh\\:mm} - {Location} (Свободно: {MaxParticipants - CurrentParticipants})";
    }
}

