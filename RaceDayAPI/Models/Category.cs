
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayAPI.Models
{
    public class Category
    {
        public int CategoryID { get; set; }

        public int EventID { get; set; }

        [Required, MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(8,2)")]
        public decimal Distance { get; set; }

        [MaxLength(20)]
        public string DistanceUnit { get; set; } = "km";

        [MaxLength(500)]
        public string? Description { get; set; }

        public int? MinAge { get; set; }

        public int? MaxAge { get; set; }

        public int MaxParticipants { get; set; } = 50;

        public int CurrentParticipants { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public Event? Event { get; set; }
        public ICollection<EventEnrolment> Enrolments { get; set; } = new List<EventEnrolment>();
    }
}