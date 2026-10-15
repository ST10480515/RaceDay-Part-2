using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.Models.Dtos
{
    public class ProfileResponseDto
    {
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateProfileDto
    {
        [Required, MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        /// <summary>Only needed when changing the password.</summary>
        public string? CurrentPassword { get; set; }

        /// <summary>Leave empty to keep the current password. Minimum 8 characters.</summary>
        [MinLength(8)]
        public string? NewPassword { get; set; }
    }
}