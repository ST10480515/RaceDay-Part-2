
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayAPI.Models
{
    public class Event
    {
        public int EventID { get; set; }

        public int OrganiserID { get; set; }

        [Required, MaxLength(200)]
        public string EventName { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        [Required, MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        // Required by the brief: "Run", "Walk" or "Cycle"
        [Required, MaxLength(20)]
        public string EventType { get; set; } = "Run";

        // Required by the brief: overall event distance
        [Column(TypeName = "decimal(8,2)")]
        public decimal Distance { get; set; }

        public DateTime? RegistrationDeadline { get; set; }

        public int MaxParticipants { get; set; } = 100;

        public int CurrentParticipants { get; set; } = 0;

        // "Upcoming", "Ongoing", "Completed", "Cancelled"
        [Required, MaxLength(50)]
        public string Status { get; set; } = "Upcoming";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public Users? Organiser { get; set; }
        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public ICollection<EventEnrolment> Enrolments { get; set; } = new List<EventEnrolment>();
    }
}