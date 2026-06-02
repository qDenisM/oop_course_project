using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_Accounting_Exams.Models
{
    public enum UniversityApplicationStatus
    {
        Pending,
        Accepted,
        Rejected,
        Waitlisted
    }

    public enum ApplicationType
    {
        Budget,
        Paid
    }

    public class UniversityApplication
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int UniversityId { get; set; }

        [Required]
        public int SpecialityId { get; set; }

        [Required]
        public UniversityApplicationStatus Status { get; set; } = UniversityApplicationStatus.Pending;

        [Required]
        public ApplicationType Type { get; set; } = ApplicationType.Budget;

        [MaxLength(500)]
        public string? Notes { get; set; }

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        public DateTime AppliedAt { get; set; } = DateTime.Now;

        public DateTime? ReviewedAt { get; set; }

        public int? ReviewedByUserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("UniversityId")]
        public virtual University University { get; set; } = null!;

        [ForeignKey("SpecialityId")]
        public virtual Speciality Speciality { get; set; } = null!;

        [ForeignKey("ReviewedByUserId")]
        public virtual User? ReviewedBy { get; set; }
    }
}

