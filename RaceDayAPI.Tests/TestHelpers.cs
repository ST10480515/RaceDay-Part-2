/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;*/

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.Models;

namespace RaceDayAPI.Tests
{
    /// <summary>
    /// Shared set-up code so each test can stay short and follow Arrange, Act, Assert.
    /// </summary>
    public static class TestHelpers
    {
        public const string TestPassword = "Password123";

        // Hashing is slow on purpose, so do it once and reuse it.
        private static readonly Lazy<string> TestPasswordHash =
            new(() => BCrypt.Net.BCrypt.HashPassword(TestPassword));

        /// <summary>A brand-new, empty in-memory database. Every test gets its own.</summary>
        public static RaceDayDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<RaceDayDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new RaceDayDbContext(options);
        }

        /// <summary>
        /// Gives a controller a fake session. Pass nothing for "not logged in",
        /// or a user ID and role for "logged in as...".
        /// </summary>
        public static T WithSession<T>(this T controller, int? userId = null, string? role = null)
            where T : ControllerBase
        {
            var session = new TestSession();
            if (userId.HasValue) session.SetInt32("UserID", userId.Value);
            if (role != null) session.SetString("Role", role);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { Session = session }
            };
            return controller;
        }

        public static Users AddUser(RaceDayDbContext db, string role)
        {
            var user = new Users
            {
                FullName = $"Test {role}",
                Email = $"{role.ToLower()}-{Guid.NewGuid():N}@test.com",
                PasswordHash = TestPasswordHash.Value,
                Role = role
            };
            db.Users.Add(user);
            db.SaveChanges();
            return user;
        }

        public static Event AddEvent(RaceDayDbContext db, int organiserId,
            string status = "Upcoming", int maxParticipants = 100,
            DateTime? registrationDeadline = null)
        {
            var ev = new Event
            {
                OrganiserID = organiserId,
                EventName = "Test Event",
                Description = "A test event",
                EventDate = DateTime.Now.AddDays(30),
                Location = "Johannesburg",
                EventType = "Run",
                Distance = 10,
                RegistrationDeadline = registrationDeadline ?? DateTime.Now.AddDays(20),
                MaxParticipants = maxParticipants,
                Status = status
            };
            db.Events.Add(ev);
            db.SaveChanges();
            return ev;
        }

        public static Category AddCategory(RaceDayDbContext db, int eventId,
            string name = "10km", int maxParticipants = 50, int currentParticipants = 0)
        {
            var category = new Category
            {
                EventID = eventId,
                CategoryName = name,
                Distance = 10,
                DistanceUnit = "km",
                MaxParticipants = maxParticipants,
                CurrentParticipants = currentParticipants
            };
            db.Categories.Add(category);
            db.SaveChanges();
            return category;
        }

        public static EventEnrolment AddEnrolment(RaceDayDbContext db, int eventId,
            int userId, int categoryId, string status = "Confirmed")
        {
            var enrolment = new EventEnrolment
            {
                EventID = eventId,
                UserID = userId,
                CategoryID = categoryId,
                Status = status,
                PaymentStatus = "Unpaid"
            };
            db.EventEnrolments.Add(enrolment);
            db.SaveChanges();
            return enrolment;
        }
    }
}