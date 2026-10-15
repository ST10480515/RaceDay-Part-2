

using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.Models
{
    public class Result
    {
        public int ResultID { get; set; }

        public int EnrolmentID { get; set; }

        public TimeSpan? FinishTime { get; set; }

        public int? FinishTimeSeconds { get; set; }

        public int? Position { get; set; }

        // "Pending", "Completed", "DNS", "Disqualified"
        [Required, MaxLength(50)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public EventEnrolment? Enrolment { get; set; }
    }
}