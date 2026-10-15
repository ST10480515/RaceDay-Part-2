using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.Models.Dtos
{
    public class ResultRequestDto
    {
        /// <summary>Finish time as hh:mm:ss, for example "01:55:15". Required when status is Completed.</summary>
        public TimeSpan? FinishTime { get; set; }

        /// <summary>Finishing position within the category. Required when status is Completed.</summary>
        [Range(1, 100000)]
        public int? Position { get; set; }

        /// <summary>Pending, Completed, DNS or Disqualified.</summary>
        [Required]
        public string Status { get; set; } = string.Empty;
    }

    public class ResultResponseDto
    {
        public int ResultID { get; set; }
        public int EnrolmentID { get; set; }
        public int EventID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int UserID { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public string? RegistrationNumber { get; set; }
        public string? FinishTime { get; set; }
        public int? FinishTimeSeconds { get; set; }
        public int? Position { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}