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
    public class ResultsController : ControllerBase
    {
        private static readonly string[] Statuses = { "Pending", "Completed", "DNS", "Disqualified" };

        private readonly RaceDayDbContext _context;

        public ResultsController(RaceDayDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Captures the result for an enrolment. Organisers only, and only for their own events.
        /// The event must be Ongoing or Completed.
        /// </summary>
        /// <param name="enrolmentId">The enrolment ID.</param>
        /// <param name="dto">Finish time, position and status. Time and position are only stored when status is Completed.</param>
        /// <returns>The created result.</returns>
        [HttpPost("enrolments/{enrolmentId}/result")]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(typeof(ResultResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateResult(int enrolmentId, [FromBody] ResultRequestDto dto)
        {
            var enrolment = await _context.EventEnrolments
                .Include(e => e.Event).Include(e => e.Category).Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EnrolmentID == enrolmentId);
            if (enrolment == null) return NotFound("Enrolment not found.");

            var organiserId = HttpContext.Session.GetInt32("UserID")!.Value;
            if (enrolment.Event!.OrganiserID != organiserId)
                return StatusCode(StatusCodes.Status403Forbidden, "You can only capture results for your own events.");

            if (enrolment.Status == "Withdrawn")
                return BadRequest("This participant withdrew from the event.");

            var error = ValidateEventAndDto(enrolment.Event, dto);
            if (error != null) return BadRequest(error);

            if (await _context.Results.AnyAsync(r => r.EnrolmentID == enrolmentId))
                return Conflict("A result already exists for this enrolment. Update it instead.");

            if (await PositionTaken(enrolment, dto, null))
                return Conflict("Another participant in this category already has that position.");

            var result = new Result { EnrolmentID = enrolmentId, Enrolment = enrolment };
            Apply(result, enrolment, dto);

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, ToDto(result));
        }

        /// <summary>
        /// Updates an existing result. Organisers only, and only for their own events.
        /// Setting the status to Disqualified also disqualifies the enrolment.
        /// </summary>
        /// <param name="id">The result ID.</param>
        /// <param name="dto">The corrected finish time, position and status.</param>
        [HttpPut("results/{id}")]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(typeof(ResultResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateResult(int id, [FromBody] ResultRequestDto dto)
        {
            var result = await LoadResult(id);
            if (result == null) return NotFound("Result not found.");

            var enrolment = result.Enrolment!;
            var organiserId = HttpContext.Session.GetInt32("UserID")!.Value;
            if (enrolment.Event!.OrganiserID != organiserId)
                return StatusCode(StatusCodes.Status403Forbidden, "You can only change results for your own events.");

            var error = ValidateEventAndDto(enrolment.Event, dto);
            if (error != null) return BadRequest(error);

            if (await PositionTaken(enrolment, dto, id))
                return Conflict("Another participant in this category already has that position.");

            Apply(result, enrolment, dto);
            await _context.SaveChangesAsync();

            return Ok(ToDto(result));
        }

        /// <summary>
        /// Lists all results for one of the Organiser's own events, best positions first.
        /// </summary>
        /// <param name="eventId">The event ID.</param>
        /// <param name="categoryId">Optional filter by category.</param>
        [HttpGet("events/{eventId}/results")]
        [SessionAuthorize("Organiser")]
        [ProducesResponseType(typeof(IEnumerable<ResultResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetResultsForEvent(int eventId, [FromQuery] int? categoryId)
        {
            var ev = await _context.Events.FindAsync(eventId);
            if (ev == null) return NotFound("Event not found.");

            var organiserId = HttpContext.Session.GetInt32("UserID")!.Value;
            if (ev.OrganiserID != organiserId)
                return StatusCode(StatusCodes.Status403Forbidden, "You can only view results for your own events.");

            var query = ResultsQuery().Where(r => r.Enrolment!.EventID == eventId);
            if (categoryId.HasValue)
                query = query.Where(r => r.Enrolment!.CategoryID == categoryId.Value);

            var results = await query.ToListAsync();
            return Ok(results
                .OrderBy(r => r.Enrolment!.CategoryID)
                .ThenBy(r => r.Position ?? int.MaxValue)
                .Select(ToDto));
        }

        /// <summary>
        /// Lists the logged-in Participant's own results.
        /// </summary>
        [HttpGet("results/my")]
        [SessionAuthorize("Participant")]
        [ProducesResponseType(typeof(IEnumerable<ResultResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyResults()
        {
            var userId = HttpContext.Session.GetInt32("UserID")!.Value;

            var results = await ResultsQuery()
                .Where(r => r.Enrolment!.UserID == userId)
                .ToListAsync();

            return Ok(results.OrderByDescending(r => r.Enrolment!.Event!.EventDate).Select(ToDto));
        }

        // ---------- helpers ----------
        private IQueryable<Result> ResultsQuery() =>
            _context.Results
                .Include(r => r.Enrolment).ThenInclude(e => e!.Event)
                .Include(r => r.Enrolment).ThenInclude(e => e!.Category)
                .Include(r => r.Enrolment).ThenInclude(e => e!.User);

        private Task<Result?> LoadResult(int id) =>
            ResultsQuery().FirstOrDefaultAsync(r => r.ResultID == id);

        private static string? ValidateEventAndDto(Event ev, ResultRequestDto dto)
        {
            if (ev.Status != "Ongoing" && ev.Status != "Completed")
                return "Results can only be captured once the event is Ongoing or Completed.";

            if (!Statuses.Contains(dto.Status))
                return "Status must be Pending, Completed, DNS or Disqualified.";

            if (dto.Status == "Completed")
            {
                if (dto.FinishTime == null || dto.FinishTime.Value <= TimeSpan.Zero)
                    return "A completed result needs a finish time greater than zero (hh:mm:ss).";
                if (dto.Position == null)
                    return "A completed result needs a finishing position.";
            }
            return null;
        }

        private async Task<bool> PositionTaken(EventEnrolment enrolment, ResultRequestDto dto, int? ignoreResultId)
        {
            if (dto.Status != "Completed" || dto.Position == null) return false;

            return await _context.Results.AnyAsync(r =>
                r.Enrolment!.EventID == enrolment.EventID &&
                r.Enrolment.CategoryID == enrolment.CategoryID &&
                r.Position == dto.Position &&
                r.ResultID != (ignoreResultId ?? 0));
        }

        private static void Apply(Result result, EventEnrolment enrolment, ResultRequestDto dto)
        {
            result.Status = dto.Status;

            if (dto.Status == "Completed")
            {
                result.FinishTime = dto.FinishTime;
                result.FinishTimeSeconds = (int)dto.FinishTime!.Value.TotalSeconds;
                result.Position = dto.Position;
            }
            else
            {
                result.FinishTime = null;
                result.FinishTimeSeconds = null;
                result.Position = null;
            }

            result.UpdatedAt = DateTime.Now;

            // Keep the enrolment status in step with the result
            if (dto.Status == "Disqualified")
                enrolment.Status = "Disqualified";
            else if (enrolment.Status == "Disqualified")
                enrolment.Status = "Confirmed";
        }

        private static ResultResponseDto ToDto(Result r) => new()
        {
            ResultID = r.ResultID,
            EnrolmentID = r.EnrolmentID,
            EventID = r.Enrolment!.EventID,
            EventName = r.Enrolment.Event?.EventName ?? string.Empty,
            CategoryID = r.Enrolment.CategoryID,
            CategoryName = r.Enrolment.Category?.CategoryName ?? string.Empty,
            UserID = r.Enrolment.UserID,
            ParticipantName = r.Enrolment.User?.FullName ?? string.Empty,
            RegistrationNumber = r.Enrolment.RegistrationNumber,
            FinishTime = r.FinishTime?.ToString(@"hh\:mm\:ss"),
            FinishTimeSeconds = r.FinishTimeSeconds,
            Position = r.Position,
            Status = r.Status
        };
    }
}