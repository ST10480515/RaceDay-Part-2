/*using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RaceDayAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
    }
}*/


using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.Models;
using RaceDayAPI.Models.Dtos;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public AuthController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Registers a new user as either an Organiser or a Participant.
        /// </summary>
        /// <param name="dto">Full name, email, password (min 8 characters) and role.</param>
        /// <returns>The created user, without the password.</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (dto.Role != "Organiser" && dto.Role != "Participant")
                return BadRequest("Role must be either 'Organiser' or 'Participant'.");

            var email = dto.Email.Trim().ToLower();

            if (await _context.Users.AnyAsync(u => u.Email == email))
                return Conflict("An account with this email already exists.");

            var user = new Users
            {
                FullName = dto.FullName.Trim(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var response = new UserResponseDto
            {
                UserID = user.UserID,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role
            };

            return StatusCode(StatusCodes.Status201Created, response);
        }

        /// <summary>
        /// Logs a user in and starts a session that stores their ID and role.
        /// </summary>
        /// <param name="dto">Email and password.</param>
        /// <returns>The logged-in user, without the password.</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var email = dto.Email.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("Invalid email or password.");

            HttpContext.Session.SetInt32("UserID", user.UserID);
            HttpContext.Session.SetString("Role", user.Role);

            return Ok(new UserResponseDto
            {
                UserID = user.UserID,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role
            });
        }

        /// <summary>
        /// Logs the current user out by clearing their session.
        /// </summary>
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok("Logged out.");
        }

        /// <summary>
        /// Returns the currently logged-in user. Requires an active session.
        /// </summary>
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Me()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
                return Unauthorized("You must be logged in.");

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null)
                return Unauthorized("You must be logged in.");

            return Ok(new UserResponseDto
            {
                UserID = user.UserID,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role
            });
        }
    }
}