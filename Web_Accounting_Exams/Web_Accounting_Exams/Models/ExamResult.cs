using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_Accounting_Exams.Models
{
    public class ExamResult
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int ExamId { get; set; }

        [Required]
        public int ExamApplicationId { get; set; }

        public int Score { get; set; }

        public int MaxScore { get; set; } = 100;

        [MaxLength(1000)]
        public string? Comments { get; set; }

        public DateTime ExamDate { get; set; }

        public DateTime ResultDate { get; set; } = DateTime.Now;

        public bool IsNotified { get; set; } = false;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; } = null!;

        [ForeignKey("ExamApplicationId")]
        public virtual ExamApplication ExamApplication { get; set; } = null!;
    }
}

