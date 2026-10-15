
using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.Models
{
    public class Users
    {
        [Key]
        
        public int UserID { get; set; }

        [Required, MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        // Stores the HASH only, never the plain-text password
        [Required, MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        // "Organiser" or "Participant"
        [Required, MaxLength(50)]
        public string Role { get; set; } = "Participant";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();
        public ICollection<EventEnrolment> Enrolments { get; set; } = new List<EventEnrolment>();
    }
}