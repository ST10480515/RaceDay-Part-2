using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.Filters;
using RaceDayAPI.Models;
using RaceDayAPI.Models.Dtos;

namespace RaceDayAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SessionAuthorize] // viewing is open to any logged-in user
    public class EventsController : ControllerBase
    {
        private static readonly string[] EventTypes = { "Run", "Walk", "Cycle" };
        private static readonly string[] Statuses = { "Upcoming", "Ongoing", "Completed", "Cancelled" };

        private readonly RaceDayDbContext _context;

        public EventsController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lists all events, soonest first. Available to both roles.
        /// </summary>
        /// <param name="eventType">Optional filter: Run, Walk or Cycle.</param>
        /// <param name="status">Optional filter: Upcoming, Ongoing, Completed or Cancelled.</param>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<EventResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetEvents([FromQuery] string? eventType, [FromQuery] string? status)
        {
            var query = _context.Events.Include(e => e.Organiser).AsQueryable();

            if (!string.IsNullOrWhiteSpace(eventType))
                query = query.Where(e => e.EventType == eventType);
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(e => e.Status == status);

            var events = await query.OrderBy(e => e.EventDate).ToListAsync();
            return Ok(events.Select(ToDto));
        }

        /// <summary>
        /// Gets one event by its ID. Available to both roles.
        /// </summary>
        /// <param name="id">The event ID.</param>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEvent(int id)
        {
            var ev = await _context.Events.Include(e => e.Organiser)
                .FirstOrDefaultAsync(e => e.EventID == id);

            if (ev == null) return NotFound("Event not found.");
            return Ok(ToDto(ev));
        }

        /// <summary>
        /// Creates a new event. Organisers only.
        /// </summary>
        /// <param name="dto">Name, description, date, location, distance, type (Run/Walk/Cycle), and limits.</param>
        /// <returns>The created event.</returns>
        [HttpPost]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateEvent([FromBody] EventRequestDto dto)
        {
            var error = Validate(dto);
            if (error == null && dto.EventDate <= DateTime.Now)
                error = "Event date must be in the future.";
            if (error != null) return BadRequest(error);

            var organiserId = HttpContext.Session.GetInt32("UserID")!.Value;

            var ev = new Event
            {
                OrganiserID = organiserId,
                EventName = dto.EventName.Trim(),
                Description = dto.Description?.Trim(),
                EventDate = dto.EventDate,
                Location = dto.Location.Trim(),
                Distance = dto.Distance,
                EventType = dto.EventType,
                RegistrationDeadline = dto.RegistrationDeadline,
                MaxParticipants = dto.MaxParticipants,
                Status = "Upcoming"
            };

            _context.Events.Add(ev);
            await _context.SaveChangesAsync();
            await _context.Entry(ev).Reference(e => e.Organiser).LoadAsync();

            return CreatedAtAction(nameof(GetEvent), new { id = ev.EventID }, ToDto(ev));
        }

        /// <summary>
        /// Updates an event. Organisers only, and only their own events.
        /// </summary>
        /// <param name="id">The event ID.</param>
        /// <param name="dto">The new event details. Status can also be changed here.</param>
        [HttpPut("{id}")]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(typeof(EventResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] EventRequestDto dto)
        {
            var ev = await _context.Events.Include(e => e.Organiser)
                .FirstOrDefaultAsync(e => e.EventID == id);
            if (ev == null) return NotFound("Event not found.");

            var organiserId = HttpContext.Session.GetInt32("UserID")!.Value;
            if (ev.OrganiserID != organiserId)
                return StatusCode(StatusCodes.Status403Forbidden, "You can only change your own events.");

            var error = Validate(dto);
            if (error == null && !string.IsNullOrWhiteSpace(dto.Status) && !Statuses.Contains(dto.Status))
                error = "Status must be Upcoming, Ongoing, Completed or Cancelled.";
            if (error == null && dto.MaxParticipants < ev.CurrentParticipants)
                error = "Max participants cannot be lower than the number already enrolled.";
            if (error != null) return BadRequest(error);

            ev.EventName = dto.EventName.Trim();
            ev.Description = dto.Description?.Trim();
            ev.EventDate = dto.EventDate;
            ev.Location = dto.Location.Trim();
            ev.Distance = dto.Distance;
            ev.EventType = dto.EventType;
            ev.RegistrationDeadline = dto.RegistrationDeadline;
            ev.MaxParticipants = dto.MaxParticipants;
            if (!string.IsNullOrWhiteSpace(dto.Status)) ev.Status = dto.Status;
            ev.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(ToDto(ev));
        }

        /// <summary>
        /// Deletes an event. Organisers only, and only their own events.
        /// Events that already have enrolments cannot be deleted; set their status to Cancelled instead.
        /// </summary>
        /// <param name="id">The event ID.</param>
        [HttpDelete("{id}")]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound("Event not found.");

            var organiserId = HttpContext.Session.GetInt32("UserID")!.Value;
            if (ev.OrganiserID != organiserId)
                return StatusCode(StatusCodes.Status403Forbidden, "You can only delete your own events.");

            if (await _context.EventEnrolments.AnyAsync(en => en.EventID == id))
                return Conflict("This event has enrolments. Set its status to Cancelled instead of deleting it.");

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ---------- helpers ----------
        private static string? Validate(EventRequestDto dto)
        {
            if (!EventTypes.Contains(dto.EventType))
                return "Event type must be Run, Walk or Cycle.";
            if (dto.RegistrationDeadline.HasValue && dto.RegistrationDeadline.Value > dto.EventDate)
                return "Registration deadline must be before the event date.";
            return null;
        }

        private static EventResponseDto ToDto(Event e) => new()
        {
            EventID = e.EventID,
            OrganiserID = e.OrganiserID,
            OrganiserName = e.Organiser?.FullName ?? string.Empty,
            EventName = e.EventName,
            Description = e.Description,
            EventDate = e.EventDate,
            Location = e.Location,
            Distance = e.Distance,
            EventType = e.EventType,
            RegistrationDeadline = e.RegistrationDeadline,
            MaxParticipants = e.MaxParticipants,
            CurrentParticipants = e.CurrentParticipants,
            Status = e.Status
        };
    }
}