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
    [SessionAuthorize]
    public class EnrolmentsController : ControllerBase
    {
        private readonly RaceDayDbContext _context;

        public EnrolmentsController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Enrols the logged-in Participant in an event by selecting a category.
        /// </summary>
        /// <param name="eventId">The event to enter.</param>
        /// <param name="dto">The chosen category ID.</param>
        /// <returns>The enrolment, including the generated registration number.</returns>
        [HttpPost("events/{eventId}/enrolments")]
        [SessionAuthorize("Participant")]
        [ProducesResponseType(typeof(EnrolmentResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Enrol(int eventId, [FromBody] EnrolRequestDto dto)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return NotFound("Event not found.");

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.CategoryID == dto.CategoryID && c.EventID == eventId);
            if (category == null)
                return BadRequest("That category does not belong to this event.");

            // Event must be open for enrolment
            if (ev.Status != "Upcoming")
                return BadRequest($"This event is {ev.Status.ToLower()} and is not open for enrolment.");

            var deadline = ev.RegistrationDeadline ?? ev.EventDate;
            if (DateTime.Now > deadline)
                return BadRequest("Registration for this event has closed.");

            // Capacity checks
            if (ev.CurrentParticipants >= ev.MaxParticipants)
                return Conflict("This event is full.");
            if (category.CurrentParticipants >= category.MaxParticipants)
                return Conflict("This category is full.");

            var userId = HttpContext.Session.GetInt32("UserID")!.Value;

            var existing = await _context.EventEnrolments
                .FirstOrDefaultAsync(e => e.EventID == eventId && e.UserID == userId);

            EventEnrolment enrolment;

            if (existing != null)
            {
                if (existing.Status != "Withdrawn")
                    return Conflict("You are already enrolled in this event.");

                // Re-activate a previously withdrawn enrolment
                existing.CategoryID = category.CategoryID;
                existing.Status = "Confirmed";
                existing.EnrolmentDate = DateTime.Now;
                existing.UpdatedAt = DateTime.Now;
                enrolment = existing;
            }
            else
            {
                enrolment = new EventEnrolment
                {
                    EventID = eventId,
                    UserID = userId,
                    CategoryID = category.CategoryID,
                    Status = "Confirmed",
                    PaymentStatus = "Unpaid"
                };
                _context.EventEnrolments.Add(enrolment);
            }

            ev.CurrentParticipants++;
            category.CurrentParticipants++;
            await _context.SaveChangesAsync();

            // Registration number needs the generated ID
            if (string.IsNullOrEmpty(enrolment.RegistrationNumber))
            {
                enrolment.RegistrationNumber = $"REG-{ev.EventDate.Year}-{enrolment.EnrolmentID:D4}";
                await _context.SaveChangesAsync();
            }

            var created = await LoadEnrolment(enrolment.EnrolmentID);
            return StatusCode(StatusCodes.Status201Created, ToDto(created!));
        }

        /// <summary>
        /// Lists the logged-in Participant's own enrolments.
        /// </summary>
        [HttpGet("enrolments/my")]
        [SessionAuthorize("Participant")]
        [ProducesResponseType(typeof(IEnumerable<EnrolmentResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var userId = HttpContext.Session.GetInt32("UserID")!.Value;

            var enrolments = await _context.EventEnrolments
                .Include(e => e.Event).Include(e => e.Category).Include(e => e.User)
                .Where(e => e.UserID == userId)
                .OrderBy(e => e.Event!.EventDate)
                .ToListAsync();

            return Ok(enrolments.Select(ToDto));
        }

        /// <summary>
        /// Lists all enrolments for one of the Organiser's own events.
        /// </summary>
        /// <param name="eventId">The event ID.</param>
        /// <param name="categoryId">Optional filter by category.</param>
        /// <param name="status">Optional filter: Pending, Confirmed, Withdrawn or Disqualified.</param>
        [HttpGet("events/{eventId}/enrolments")]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(typeof(IEnumerable<EnrolmentResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEnrolmentsForEvent(
            int eventId, [FromQuery] int? categoryId, [FromQuery] string? status)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return NotFound("Event not found.");

            var organiserId = HttpContext.Session.GetInt32("UserID")!.Value;
            if (ev.OrganiserID != organiserId)
                return StatusCode(StatusCodes.Status403Forbidden, "You can only view enrolments for your own events.");

            var query = _context.EventEnrolments
                .Include(e => e.Event).Include(e => e.Category).Include(e => e.User)
                .Where(e => e.EventID == eventId);

            if (categoryId.HasValue) query = query.Where(e => e.CategoryID == categoryId.Value);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(e => e.Status == status);

            var enrolments = await query.OrderBy(e => e.EnrolmentDate).ToListAsync();
            return Ok(enrolments.Select(ToDto));
        }

        /// <summary>
        /// Withdraws the logged-in Participant from an event. Only their own enrolment can be withdrawn.
        /// </summary>
        /// <param name="id">The enrolment ID.</param>
        [HttpDelete("enrolments/{id}")]
        [SessionAuthorize("Participant")]
        [ProducesResponseType(typeof(EnrolmentResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Withdraw(int id)
        {
            var enrolment = await LoadEnrolment(id);
            if (enrolment == null) return NotFound("Enrolment not found.");

            var userId = HttpContext.Session.GetInt32("UserID")!.Value;
            if (enrolment.UserID != userId)
                return StatusCode(StatusCodes.Status403Forbidden, "You can only withdraw your own enrolment.");

            if (enrolment.Status == "Withdrawn")
                return BadRequest("You have already withdrawn from this event.");
            if (enrolment.Status == "Disqualified")
                return BadRequest("A disqualified enrolment cannot be withdrawn.");
            if (enrolment.Event!.Status != "Upcoming")
                return BadRequest("You can only withdraw from an upcoming event.");

            enrolment.Status = "Withdrawn";
            enrolment.UpdatedAt = DateTime.Now;
            enrolment.Event.CurrentParticipants = Math.Max(0, enrolment.Event.CurrentParticipants - 1);
            enrolment.Category!.CurrentParticipants = Math.Max(0, enrolment.Category.CurrentParticipants - 1);

            await _context.SaveChangesAsync();
            return Ok(ToDto(enrolment));
        }

        // ---------- helpers ----------
        private Task<EventEnrolment?> LoadEnrolment(int id) =>
            _context.EventEnrolments
                .Include(e => e.Event).Include(e => e.Category).Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EnrolmentID == id);

        private static EnrolmentResponseDto ToDto(EventEnrolment e) => new()
        {
            EnrolmentID = e.EnrolmentID,
            EventID = e.EventID,
            EventName = e.Event?.EventName ?? string.Empty,
            EventDate = e.Event?.EventDate ?? default,
            CategoryID = e.CategoryID,
            CategoryName = e.Category?.CategoryName ?? string.Empty,
            UserID = e.UserID,
            ParticipantName = e.User?.FullName ?? string.Empty,
            ParticipantEmail = e.User?.Email ?? string.Empty,
            EnrolmentDate = e.EnrolmentDate,
            Status = e.Status,
            PaymentStatus = e.PaymentStatus,
            RegistrationNumber = e.RegistrationNumber
        };
    }
}