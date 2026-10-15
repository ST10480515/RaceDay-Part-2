using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.Filters;
using RaceDayAPI.Models;
using RaceDayAPI.Models.Dtos;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    [Route("api")]
    [SessionAuthorize] // viewing is open to any logged-in user
    public class CategoriesController : ControllerBase
    {
        private static readonly string[] Units = { "km", "mi" };

        private readonly RaceDayDbContext _context;

        public CategoriesController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lists all categories of an event. Available to both roles.
        /// </summary>
        /// <param name="eventId">The event ID.</param>
        [HttpGet("events/{eventId}/categories")]
        [ProducesResponseType(typeof(IEnumerable<CategoryResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCategoriesForEvent(int eventId)
        {
            if (!await _context.Events.AnyAsync(e => e.EventID == eventId))
                return NotFound("Event not found.");

            var categories = await _context.Categories
                .Where(c => c.EventID == eventId)
                .OrderBy(c => c.Distance).ThenBy(c => c.CategoryName)
                .ToListAsync();

            return Ok(categories.Select(ToDto));
        }

        /// <summary>
        /// Gets one category by its ID. Available to both roles.
        /// </summary>
        /// <param name="id">The category ID.</param>
        [HttpGet("categories/{id}")]
        [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound("Category not found.");
            return Ok(ToDto(category));
        }

        /// <summary>
        /// Adds a category to an event. Organisers only, and only for their own events.
        /// </summary>
        /// <param name="eventId">The event ID.</param>
        /// <param name="dto">Name, distance, optional age range and participant limit.</param>
        /// <returns>The created category.</returns>
        [HttpPost("events/{eventId}/categories")]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateCategory(int eventId, [FromBody] CategoryRequestDto dto)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return NotFound("Event not found.");

            var organiserId = HttpContext.Session.GetInt32("UserID")!.Value;
            if (ev.OrganiserID != organiserId)
                return StatusCode(StatusCodes.Status403Forbidden, "You can only add categories to your own events.");

            if (ev.Status == "Completed" || ev.Status == "Cancelled")
                return BadRequest("Categories cannot be added to a completed or cancelled event.");

            var error = Validate(dto);
            if (error != null) return BadRequest(error);

            var name = dto.CategoryName.Trim();
            if (await _context.Categories.AnyAsync(c => c.EventID == eventId && c.CategoryName == name))
                return Conflict("This event already has a category with that name.");

            var category = new Category
            {
                EventID = eventId,
                CategoryName = name,
                Distance = dto.Distance,
                DistanceUnit = dto.DistanceUnit,
                Description = dto.Description?.Trim(),
                MinAge = dto.MinAge,
                MaxAge = dto.MaxAge,
                MaxParticipants = dto.MaxParticipants
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryID }, ToDto(category));
        }

        /// <summary>
        /// Updates a category. Organisers only, and only for their own events.
        /// </summary>
        /// <param name="id">The category ID.</param>
        /// <param name="dto">The new category details.</param>
        [HttpPut("categories/{id}")]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(typeof(CategoryResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryRequestDto dto)
        {
            var category = await _context.Categories.Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == id);
            if (category == null) return NotFound("Category not found.");

            var organiserId = HttpContext.Session.GetInt32("UserID")!.Value;
            if (category.Event!.OrganiserID != organiserId)
                return StatusCode(StatusCodes.Status403Forbidden, "You can only change categories of your own events.");

            var error = Validate(dto);
            if (error == null && dto.MaxParticipants < category.CurrentParticipants)
                error = "Max participants cannot be lower than the number already enrolled.";
            if (error != null) return BadRequest(error);

            var name = dto.CategoryName.Trim();
            if (await _context.Categories.AnyAsync(c =>
                    c.EventID == category.EventID && c.CategoryName == name && c.CategoryID != id))
                return Conflict("This event already has a category with that name.");

            category.CategoryName = name;
            category.Distance = dto.Distance;
            category.DistanceUnit = dto.DistanceUnit;
            category.Description = dto.Description?.Trim();
            category.MinAge = dto.MinAge;
            category.MaxAge = dto.MaxAge;
            category.MaxParticipants = dto.MaxParticipants;

            await _context.SaveChangesAsync();
            return Ok(ToDto(category));
        }

        /// <summary>
        /// Deletes a category. Organisers only, and only for their own events.
        /// Categories that already have enrolments cannot be deleted.
        /// </summary>
        /// <param name="id">The category ID.</param>
        [HttpDelete("categories/{id}")]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.Include(c => c.Event)
                .FirstOrDefaultAsync(c => c.CategoryID == id);
            if (category == null) return NotFound("Category not found.");

            var organiserId = HttpContext.Session.GetInt32("UserID")!.Value;
            if (category.Event!.OrganiserID != organiserId)
                return StatusCode(StatusCodes.Status403Forbidden, "You can only delete categories of your own events.");

            if (await _context.EventEnrolments.AnyAsync(en => en.CategoryID == id))
                return Conflict("This category has enrolments and cannot be deleted.");

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ---------- helpers ----------
        private static string? Validate(CategoryRequestDto dto)
        {
            if (!Units.Contains(dto.DistanceUnit))
                return "Distance unit must be 'km' or 'mi'.";
            if (dto.MinAge.HasValue && dto.MaxAge.HasValue && dto.MinAge > dto.MaxAge)
                return "Minimum age cannot be greater than maximum age.";
            return null;
        }

        private static CategoryResponseDto ToDto(Category c) => new()
        {
            CategoryID = c.CategoryID,
            EventID = c.EventID,
            CategoryName = c.CategoryName,
            Distance = c.Distance,
            DistanceUnit = c.DistanceUnit,
            Description = c.Description,
            MinAge = c.MinAge,
            MaxAge = c.MaxAge,
            MaxParticipants = c.MaxParticipants,
            CurrentParticipants = c.CurrentParticipants,
            SpotsRemaining = c.MaxParticipants - c.CurrentParticipants
        };
    }
}