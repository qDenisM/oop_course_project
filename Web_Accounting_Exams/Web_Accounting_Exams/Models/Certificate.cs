using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_Accounting_Exams.Models
{
    public enum CertificateType
    {
        CT,          
        Diploma       
    }

    public enum CertificateStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class Certificate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public CertificateType Type { get; set; }

        [Required]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        public int Score { get; set; }

        [Required]
        [MaxLength(100)]
        public string CertificateNumber { get; set; } = string.Empty;

        public DateTime? IssueDate { get; set; }

        [MaxLength(500)]
        public string? FilePath { get; set; }

        [MaxLength(500)]
        public string? FileName { get; set; }

        [Required]
        public CertificateStatus Status { get; set; } = CertificateStatus.Pending;

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        public DateTime? ReviewedAt { get; set; }

        public int? ReviewedByUserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("ReviewedByUserId")]
        public virtual User? ReviewedBy { get; set; }
    }
}

