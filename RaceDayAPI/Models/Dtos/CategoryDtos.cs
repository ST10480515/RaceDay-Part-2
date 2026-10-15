using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.Models.Dtos
{
    public class CategoryRequestDto
    {
        /// <summary>For example "Under 20", "Senior", "10km" or "21km".</summary>
        [Required, MaxLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [Range(0.01, 1000)]
        public decimal Distance { get; set; }

        /// <summary>km or mi. Defaults to km.</summary>
        [MaxLength(20)]
        public string DistanceUnit { get; set; } = "km";

        [MaxLength(500)]
        public string? Description { get; set; }

        [Range(0, 120)]
        public int? MinAge { get; set; }

        [Range(0, 120)]
        public int? MaxAge { get; set; }

        [Range(1, 100000)]
        public int MaxParticipants { get; set; } = 50;
    }

    public class CategoryResponseDto
    {
        public int CategoryID { get; set; }
        public int EventID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Distance { get; set; }
        public string DistanceUnit { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public int MaxParticipants { get; set; }
        public int CurrentParticipants { get; set; }
        public int SpotsRemaining { get; set; }
    }
}