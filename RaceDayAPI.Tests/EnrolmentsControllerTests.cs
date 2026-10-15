/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
*/
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDayAPI.Controllers;
using RaceDayAPI.Data;
using RaceDayAPI.Models;
using RaceDayAPI.Models.Dtos;
using Xunit;

namespace RaceDayAPI.Tests
{
    public class EnrolmentsControllerTests
    {
        /// <summary>
        /// Creates a fresh database containing one organiser, one participant,
        /// one event and one category.
        /// </summary>
        private static (RaceDayDbContext Db, Users Organiser, Users Participant, Event Ev, Category Cat) Seed(
            string eventStatus = "Upcoming",
            int eventMax = 100,
            int categoryMax = 50,
            int categoryCurrent = 0,
            DateTime? deadline = null)
        {
            var db = TestHelpers.CreateContext();
            var organiser = TestHelpers.AddUser(db, "Organiser");
            var participant = TestHelpers.AddUser(db, "Participant");
            var ev = TestHelpers.AddEvent(db, organiser.UserID, eventStatus, eventMax, deadline);
            var cat = TestHelpers.AddCategory(db, ev.EventID, "10km", categoryMax, categoryCurrent);
            return (db, organiser, participant, ev, cat);
        }

        // ---------- Enrol: success ----------

        [Fact]
        public async Task Enrol_ValidParticipant_ReturnsCreated()
        {
            // Arrange
            var (db, _, participant, ev, cat) = Seed();
            var controller = new EnrolmentsController(db).WithSession(participant.UserID, "Participant");

            // Act
            var result = await controller.Enrol(ev.EventID, new EnrolRequestDto { CategoryID = cat.CategoryID });

            // Assert
            var created = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        }

        [Fact]
        public async Task Enrol_ValidParticipant_RecordsParticipantEventAndCategory()
        {
            // Arrange
            var (db, _, participant, ev, cat) = Seed();
            var controller = new EnrolmentsController(db).WithSession(participant.UserID, "Participant");

            // Act
            await controller.Enrol(ev.EventID, new EnrolRequestDto { CategoryID = cat.CategoryID });

            // Assert
            var saved = db.EventEnrolments.Single();
            Assert.Equal(participant.UserID, saved.UserID);
            Assert.Equal(ev.EventID, saved.EventID);
            Assert.Equal(cat.CategoryID, saved.CategoryID);
            Assert.Equal("Confirmed", saved.Status);
            Assert.StartsWith("REG-", saved.RegistrationNumber);
        }

        [Fact]
        public async Task Enrol_ValidParticipant_IncrementsEventAndCategoryCounts()
        {
            // Arrange
            var (db, _, participant, ev, cat) = Seed();
            var controller = new EnrolmentsController(db).WithSession(participant.UserID, "Participant");

            // Act
            await controller.Enrol(ev.EventID, new EnrolRequestDto { CategoryID = cat.CategoryID });

            // Assert
            Assert.Equal(1, db.Events.Single().CurrentParticipants);
            Assert.Equal(1, db.Categories.Single().CurrentParticipants);
        }

        // ---------- Enrol: failure ----------

        [Fact]
        public async Task Enrol_AlreadyEnrolled_ReturnsConflict()
        {
            // Arrange
            var (db, _, participant, ev, cat) = Seed();
            TestHelpers.AddEnrolment(db, ev.EventID, participant.UserID, cat.CategoryID);
            var controller = new EnrolmentsController(db).WithSession(participant.UserID, "Participant");

            // Act
            var result = await controller.Enrol(ev.EventID, new EnrolRequestDto { CategoryID = cat.CategoryID });

            // Assert
            Assert.IsType<ConflictObjectResult>(result);
            Assert.Single(db.EventEnrolments);
        }

        [Fact]
        public async Task Enrol_CategoryFromAnotherEvent_ReturnsBadRequest()
        {
            // Arrange
            var (db, organiser, participant, ev, _) = Seed();
            var otherEvent = TestHelpers.AddEvent(db, organiser.UserID);
            var otherCategory = TestHelpers.AddCategory(db, otherEvent.EventID, "5km");
            var controller = new EnrolmentsController(db).WithSession(participant.UserID, "Participant");

            // Act
            var result = await controller.Enrol(ev.EventID, new EnrolRequestDto { CategoryID = otherCategory.CategoryID });

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Empty(db.EventEnrolments);
        }

        [Fact]
        public async Task Enrol_FullCategory_ReturnsConflict()
        {
            // Arrange
            var (db, _, participant, ev, cat) = Seed(categoryMax: 1, categoryCurrent: 1);
            var controller = new EnrolmentsController(db).WithSession(participant.UserID, "Participant");

            // Act
            var result = await controller.Enrol(ev.EventID, new EnrolRequestDto { CategoryID = cat.CategoryID });

            // Assert
            Assert.IsType<ConflictObjectResult>(result);
            Assert.Empty(db.EventEnrolments);
        }

        [Fact]
        public async Task Enrol_RegistrationClosed_ReturnsBadRequest()
        {
            // Arrange
            var (db, _, participant, ev, cat) = Seed(deadline: DateTime.Now.AddDays(-1));
            var controller = new EnrolmentsController(db).WithSession(participant.UserID, "Participant");

            // Act
            var result = await controller.Enrol(ev.EventID, new EnrolRequestDto { CategoryID = cat.CategoryID });

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Enrol_CancelledEvent_ReturnsBadRequest()
        {
            // Arrange
            var (db, _, participant, ev, cat) = Seed(eventStatus: "Cancelled");
            var controller = new EnrolmentsController(db).WithSession(participant.UserID, "Participant");

            // Act
            var result = await controller.Enrol(ev.EventID, new EnrolRequestDto { CategoryID = cat.CategoryID });

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Enrol_UnknownEvent_ReturnsNotFound()
        {
            // Arrange
            var (db, _, participant, _, cat) = Seed();
            var controller = new EnrolmentsController(db).WithSession(participant.UserID, "Participant");

            // Act
            var result = await controller.Enrol(9999, new EnrolRequestDto { CategoryID = cat.CategoryID });

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        // ---------- Withdraw ----------

        [Fact]
        public async Task Withdraw_OwnEnrolment_ReturnsOkAndFreesTheSpot()
        {
            // Arrange
            var (db, _, participant, ev, cat) = Seed();
            var enrolment = TestHelpers.AddEnrolment(db, ev.EventID, participant.UserID, cat.CategoryID);
            ev.CurrentParticipants = 1;
            cat.CurrentParticipants = 1;
            db.SaveChanges();
            var controller = new EnrolmentsController(db).WithSession(participant.UserID, "Participant");

            // Act
            var result = await controller.Withdraw(enrolment.EnrolmentID);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Withdrawn", db.EventEnrolments.Single().Status);
            Assert.Equal(0, db.Events.Single().CurrentParticipants);
            Assert.Equal(0, db.Categories.Single().CurrentParticipants);
        }

        [Fact]
        public async Task Withdraw_AnotherParticipantsEnrolment_ReturnsForbidden()
        {
            // Arrange
            var (db, _, participant, ev, cat) = Seed();
            var otherParticipant = TestHelpers.AddUser(db, "Participant");
            var enrolment = TestHelpers.AddEnrolment(db, ev.EventID, participant.UserID, cat.CategoryID);
            var controller = new EnrolmentsController(db).WithSession(otherParticipant.UserID, "Participant");

            // Act
            var result = await controller.Withdraw(enrolment.EnrolmentID);

            // Assert
            var forbidden = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
            Assert.Equal("Confirmed", db.EventEnrolments.Single().Status);
        }

        // ---------- Viewing enrolments ----------

        [Fact]
        public async Task GetEnrolmentsForEvent_OwningOrganiser_ReturnsEnrolments()
        {
            // Arrange
            var (db, organiser, participant, ev, cat) = Seed();
            TestHelpers.AddEnrolment(db, ev.EventID, participant.UserID, cat.CategoryID);
            var controller = new EnrolmentsController(db).WithSession(organiser.UserID, "Organiser");

            // Act
            var result = await controller.GetEnrolmentsForEvent(ev.EventID, null, null);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<EnrolmentResponseDto>>(ok.Value);
            Assert.Single(list);
        }

        [Fact]
        public async Task GetEnrolmentsForEvent_AnotherOrganiser_ReturnsForbidden()
        {
            // Arrange
            var (db, _, participant, ev, cat) = Seed();
            var otherOrganiser = TestHelpers.AddUser(db, "Organiser");
            TestHelpers.AddEnrolment(db, ev.EventID, participant.UserID, cat.CategoryID);
            var controller = new EnrolmentsController(db).WithSession(otherOrganiser.UserID, "Organiser");

            // Act
            var result = await controller.GetEnrolmentsForEvent(ev.EventID, null, null);

            // Assert
            var forbidden = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        }

        [Fact]
        public async Task GetMyEnrolments_Participant_ReturnsOnlyTheirOwn()
        {
            // Arrange
            var (db, _, participant, ev, cat) = Seed();
            var otherParticipant = TestHelpers.AddUser(db, "Participant");
            TestHelpers.AddEnrolment(db, ev.EventID, participant.UserID, cat.CategoryID);
            TestHelpers.AddEnrolment(db, ev.EventID, otherParticipant.UserID, cat.CategoryID);
            var controller = new EnrolmentsController(db).WithSession(participant.UserID, "Participant");

            // Act
            var result = await controller.GetMyEnrolments();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<EnrolmentResponseDto>>(ok.Value).ToList();
            Assert.Single(list);
            Assert.Equal(participant.UserID, list[0].UserID);
        }
    }
}