/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
*/
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDayAPI.Controllers;
using RaceDayAPI.Models.Dtos;
using Xunit;

namespace RaceDayAPI.Tests
{
    public class EventsControllerTests
    {
        private static EventRequestDto ValidEventRequest() => new()
        {
            EventName = "Joburg 10km Fun Run",
            Description = "A friendly run through the city",
            EventDate = DateTime.Now.AddDays(40),
            Location = "Johannesburg",
            Distance = 10,
            EventType = "Run",
            RegistrationDeadline = DateTime.Now.AddDays(30),
            MaxParticipants = 200
        };

        // ---------- Create ----------

        [Fact]
        public async Task CreateEvent_ValidOrganiser_ReturnsCreated()
        {
            // Arrange
            using var db = TestHelpers.CreateContext();
            var organiser = TestHelpers.AddUser(db, "Organiser");
            var controller = new EventsController(db).WithSession(organiser.UserID, "Organiser");

            // Act
            var result = await controller.CreateEvent(ValidEventRequest());

            // Assert
            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task CreateEvent_ValidOrganiser_SavesEventUnderThatOrganiser()
        {
            // Arrange
            using var db = TestHelpers.CreateContext();
            var organiser = TestHelpers.AddUser(db, "Organiser");
            var controller = new EventsController(db).WithSession(organiser.UserID, "Organiser");

            // Act
            await controller.CreateEvent(ValidEventRequest());

            // Assert
            var saved = db.Events.Single();
            Assert.Equal(organiser.UserID, saved.OrganiserID);
            Assert.Equal("Upcoming", saved.Status);
        }

        [Fact]
        public async Task CreateEvent_InvalidEventType_ReturnsBadRequest()
        {
            // Arrange
            using var db = TestHelpers.CreateContext();
            var organiser = TestHelpers.AddUser(db, "Organiser");
            var controller = new EventsController(db).WithSession(organiser.UserID, "Organiser");
            var dto = ValidEventRequest();
            dto.EventType = "Swim";

            // Act
            var result = await controller.CreateEvent(dto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Empty(db.Events);
        }

        [Fact]
        public async Task CreateEvent_DateInThePast_ReturnsBadRequest()
        {
            // Arrange
            using var db = TestHelpers.CreateContext();
            var organiser = TestHelpers.AddUser(db, "Organiser");
            var controller = new EventsController(db).WithSession(organiser.UserID, "Organiser");
            var dto = ValidEventRequest();
            dto.EventDate = DateTime.Now.AddDays(-1);
            dto.RegistrationDeadline = null;

            // Act
            var result = await controller.CreateEvent(dto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ---------- Update ----------

        [Fact]
        public async Task UpdateEvent_OwnEvent_ReturnsOkAndSavesChanges()
        {
            // Arrange
            using var db = TestHelpers.CreateContext();
            var organiser = TestHelpers.AddUser(db, "Organiser");
            var ev = TestHelpers.AddEvent(db, organiser.UserID);
            var controller = new EventsController(db).WithSession(organiser.UserID, "Organiser");
            var dto = ValidEventRequest();
            dto.EventName = "Renamed Event";

            // Act
            var result = await controller.UpdateEvent(ev.EventID, dto);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Renamed Event", db.Events.Single().EventName);
        }

        [Fact]
        public async Task UpdateEvent_AnotherOrganisersEvent_ReturnsForbidden()
        {
            // Arrange
            using var db = TestHelpers.CreateContext();
            var owner = TestHelpers.AddUser(db, "Organiser");
            var otherOrganiser = TestHelpers.AddUser(db, "Organiser");
            var ev = TestHelpers.AddEvent(db, owner.UserID);
            var controller = new EventsController(db).WithSession(otherOrganiser.UserID, "Organiser");

            // Act
            var result = await controller.UpdateEvent(ev.EventID, ValidEventRequest());

            // Assert
            var forbidden = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
            Assert.Equal("Test Event", db.Events.Single().EventName);
        }

        [Fact]
        public async Task UpdateEvent_UnknownEvent_ReturnsNotFound()
        {
            // Arrange
            using var db = TestHelpers.CreateContext();
            var organiser = TestHelpers.AddUser(db, "Organiser");
            var controller = new EventsController(db).WithSession(organiser.UserID, "Organiser");

            // Act
            var result = await controller.UpdateEvent(9999, ValidEventRequest());

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        // ---------- Delete ----------

        [Fact]
        public async Task DeleteEvent_OwnEventWithNoEnrolments_ReturnsNoContent()
        {
            // Arrange
            using var db = TestHelpers.CreateContext();
            var organiser = TestHelpers.AddUser(db, "Organiser");
            var ev = TestHelpers.AddEvent(db, organiser.UserID);
            var controller = new EventsController(db).WithSession(organiser.UserID, "Organiser");

            // Act
            var result = await controller.DeleteEvent(ev.EventID);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Empty(db.Events);
        }

        [Fact]
        public async Task DeleteEvent_AnotherOrganisersEvent_ReturnsForbidden()
        {
            // Arrange
            using var db = TestHelpers.CreateContext();
            var owner = TestHelpers.AddUser(db, "Organiser");
            var otherOrganiser = TestHelpers.AddUser(db, "Organiser");
            var ev = TestHelpers.AddEvent(db, owner.UserID);
            var controller = new EventsController(db).WithSession(otherOrganiser.UserID, "Organiser");

            // Act
            var result = await controller.DeleteEvent(ev.EventID);

            // Assert
            var forbidden = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
            Assert.Single(db.Events);
        }

        [Fact]
        public async Task DeleteEvent_EventWithEnrolments_ReturnsConflict()
        {
            // Arrange
            using var db = TestHelpers.CreateContext();
            var organiser = TestHelpers.AddUser(db, "Organiser");
            var participant = TestHelpers.AddUser(db, "Participant");
            var ev = TestHelpers.AddEvent(db, organiser.UserID);
            var category = TestHelpers.AddCategory(db, ev.EventID);
            TestHelpers.AddEnrolment(db, ev.EventID, participant.UserID, category.CategoryID);
            var controller = new EventsController(db).WithSession(organiser.UserID, "Organiser");

            // Act
            var result = await controller.DeleteEvent(ev.EventID);

            // Assert
            Assert.IsType<ConflictObjectResult>(result);
            Assert.Single(db.Events);
        }

        // ---------- View ----------

        [Fact]
        public async Task GetEvents_ParticipantLoggedIn_ReturnsAllEvents()
        {
            // Arrange
            using var db = TestHelpers.CreateContext();
            var organiser = TestHelpers.AddUser(db, "Organiser");
            var participant = TestHelpers.AddUser(db, "Participant");
            TestHelpers.AddEvent(db, organiser.UserID);
            TestHelpers.AddEvent(db, organiser.UserID);
            var controller = new EventsController(db).WithSession(participant.UserID, "Participant");

            // Act
            var result = await controller.GetEvents(null, null);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var events = Assert.IsAssignableFrom<IEnumerable<EventResponseDto>>(ok.Value);
            Assert.Equal(2, events.Count());
        }

        [Fact]
        public async Task GetEvent_UnknownId_ReturnsNotFound()
        {
            // Arrange
            using var db = TestHelpers.CreateContext();
            var participant = TestHelpers.AddUser(db, "Participant");
            var controller = new EventsController(db).WithSession(participant.UserID, "Participant");

            // Act
            var result = await controller.GetEvent(9999);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}