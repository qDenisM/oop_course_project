using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web_Accounting_Exams.Models
{
    public class Speciality
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UniversityId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Code { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public int? BudgetPlaces { get; set; }

        public int? PaidPlaces { get; set; }

        public int? PassingScoreBudget { get; set; }

        public int? PassingScorePaid { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("UniversityId")]
        public virtual University University { get; set; } = null!;

        public virtual ICollection<SpecialitySubject> Subjects { get; set; } = new List<SpecialitySubject>();
        public virtual ICollection<UniversityApplication> Applications { get; set; } = new List<UniversityApplication>();
    }
}

