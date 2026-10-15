
using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.Models
{
    public class EventEnrolment
    {
        [Key]
        public int EnrolmentID { get; set; }

        public int EventID { get; set; }

        public int UserID { get; set; }

        public int CategoryID { get; set; }

        public DateTime EnrolmentDate { get; set; } = DateTime.Now;

        // "Pending", "Confirmed", "Withdrawn", "Disqualified"
        [Required, MaxLength(50)]
        public string Status { get; set; } = "Pending";

        // "Unpaid", "Paid", "Refunded"
        [Required, MaxLength(50)]
        public string PaymentStatus { get; set; } = "Unpaid";

        [MaxLength(50)]
        public string? RegistrationNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public Event? Event { get; set; }
        public Users? User { get; set; }
        public Category? Category { get; set; }
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<Result> Results { get; set; } = new List<Result>();
    }
}