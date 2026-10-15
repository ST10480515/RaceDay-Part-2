using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.Models.Dtos
{
    public class EventRequestDto
    {
        [Required, MaxLength(200)]
        public string EventName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        [Required, MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        /// <summary>Overall event distance (km).</summary>
        [Range(0.01, 1000)]
        public decimal Distance { get; set; }

        /// <summary>Run, Walk or Cycle.</summary>
        [Required]
        public string EventType { get; set; } = string.Empty;

        public DateTime? RegistrationDeadline { get; set; }

        [Range(1, 100000)]
        public int MaxParticipants { get; set; } = 100;

        /// <summary>Update only: Upcoming, Ongoing, Completed or Cancelled. Ignored when creating.</summary>
        public string? Status { get; set; }
    }

    public class EventResponseDto
    {
        public int EventID { get; set; }
        public int OrganiserID { get; set; }
        public string OrganiserName { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal Distance { get; set; }
        public string EventType { get; set; } = string.Empty;
        public DateTime? RegistrationDeadline { get; set; }
        public int MaxParticipants { get; set; }
        public int CurrentParticipants { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}