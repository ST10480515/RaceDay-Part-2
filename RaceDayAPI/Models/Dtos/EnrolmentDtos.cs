using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.Models.Dtos
{
    public class EnrolRequestDto
    {
        /// <summary>The category the participant wants to enter. It must belong to the event.</summary>
        [Required]
        public int CategoryID { get; set; }
    }

    public class EnrolmentResponseDto
    {
        public int EnrolmentID { get; set; }
        public int EventID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int UserID { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string ParticipantEmail { get; set; } = string.Empty;
        public DateTime EnrolmentDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string? RegistrationNumber { get; set; }
    }
}