using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.Filters;
using RaceDayAPI.Models.Dtos;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SessionAuthorize] // any logged-in user, Organiser or Participant
    public class ProfileController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public ProfileController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Returns the profile of the logged-in user.
        /// </summary>
        /// <returns>Name, email, role and the date the account was created.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(ProfileResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserID")!.Value;
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                HttpContext.Session.Clear();
                return Unauthorized("Account no longer exists.");
            }

            return Ok(ToDto(user));
        }

        /// <summary>
        /// Updates the logged-in user's name and email. Optionally changes the password.
        /// </summary>
        /// <param name="dto">New name and email. To change the password, also send the current and new password.</param>
        /// <returns>The updated profile.</returns>
        [HttpPut]
        [ProducesResponseType(typeof(ProfileResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = HttpContext.Session.GetInt32("UserID")!.Value;
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                HttpContext.Session.Clear();
                return Unauthorized("Account no longer exists.");
            }

            var email = dto.Email.Trim().ToLower();

            // Email must not belong to someone else
            if (await _context.Users.AnyAsync(u => u.Email == email && u.UserID != userId))
                return Conflict("Another account already uses this email.");

            // Optional password change
            if (!string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                if (string.IsNullOrWhiteSpace(dto.CurrentPassword) ||
                    !BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                    return BadRequest("Current password is missing or incorrect.");

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            }

            user.FullName = dto.FullName.Trim();
            user.Email = email;

            await _context.SaveChangesAsync();

            return Ok(ToDto(user));
        }

        private static ProfileResponseDto ToDto(Models.Users user) => new()
        {
            UserID = user.UserID,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };
    }
}