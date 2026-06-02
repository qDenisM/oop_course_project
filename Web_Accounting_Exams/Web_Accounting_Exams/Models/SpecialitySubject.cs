using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_Accounting_Exams.Models
{
    public class SpecialitySubject
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SpecialityId { get; set; }

        [Required]
        [MaxLength(100)]
        public string SubjectName { get; set; } = string.Empty;

        public int? MinScore { get; set; }

        public bool IsRequired { get; set; } = true;

        [ForeignKey("SpecialityId")]
        public virtual Speciality Speciality { get; set; } = null!;
    }
}

