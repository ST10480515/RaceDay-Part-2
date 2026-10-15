using System.ComponentModel.DataAnnotations;

namespace RaceDayAPI.Models.Dtos
{
    public class RegisterDto
    {
        [Required, MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string Password { get; set; } = string.Empty;

        /// <summary>Either "Organiser" or "Participant".</summary>
        [Required]
        public string Role { get; set; } = string.Empty;
    }

    public class UserResponseDto
    {
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}