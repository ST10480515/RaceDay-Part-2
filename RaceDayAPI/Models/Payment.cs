
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDayAPI.Models
{
    public class Payment
    {
        public int PaymentID { get; set; }

        public int EnrolmentID { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [MaxLength(3)]
        public string Currency { get; set; } = "ZAR";

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [Required, MaxLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        // "Pending", "Completed", "Failed", "Refunded"
        [Required, MaxLength(50)]
        public string PaymentStatus { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public EventEnrolment? Enrolment { get; set; }
    }
}