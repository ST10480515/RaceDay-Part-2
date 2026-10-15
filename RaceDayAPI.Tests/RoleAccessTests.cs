using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using RaceDayAPI.Controllers;
using RaceDayAPI.Filters;
using Xunit;

namespace RaceDayAPI.Tests
{
    /// <summary>
    /// Proves that role-based access works. Two parts:
    /// 1. The filter itself lets the right people in and rejects everyone else.
    /// 2. Every protected endpoint is marked with the correct role.
    /// </summary>
    public class RoleAccessTests
    {
        private static AuthorizationFilterContext BuildContext(int? userId, string? role)
        {
            var session = new TestSession();
            if (userId.HasValue) session.SetInt32("UserID", userId.Value);
            if (role != null) session.SetString("Role", role);

            var httpContext = new DefaultHttpContext { Session = session };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        }

        // ---------- Part 1: the filter ----------

        [Fact]
        public void Filter_NotLoggedIn_ReturnsUnauthorized()
        {
            // Arrange
            var context = BuildContext(null, null);
            var filter = new SessionAuthorizeAttribute("Organiser");

            // Act
            filter.OnAuthorization(context);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(context.Result);
        }

        [Theory]
        [InlineData("Organiser")]
        [InlineData("Participant")]
        public void Filter_NoRoleRestriction_AllowsAnyLoggedInUser(string role)
        {
            // Arrange
            var context = BuildContext(1, role);
            var filter = new SessionAuthorizeAttribute();

            // Act
            filter.OnAuthorization(context);

            // Assert
            Assert.Null(context.Result);
        }

        [Theory]
        [InlineData("Organiser", "Organiser")]
        [InlineData("Participant", "Participant")]
        public void Filter_MatchingRole_AllowsAccess(string userRole, string requiredRole)
        {
            // Arrange
            var context = BuildContext(1, userRole);
            var filter = new SessionAuthorizeAttribute(requiredRole);

            // Act
            filter.OnAuthorization(context);

            // Assert
            Assert.Null(context.Result);
        }

        [Theory]
        [InlineData("Participant", "Organiser")]
        [InlineData("Organiser", "Participant")]
        public void Filter_WrongRole_ReturnsForbidden(string userRole, string requiredRole)
        {
            // Arrange
            var context = BuildContext(1, userRole);
            var filter = new SessionAuthorizeAttribute(requiredRole);

            // Act
            filter.OnAuthorization(context);

            // Assert
            var result = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        }

        // ---------- Part 2: every endpoint carries the right role ----------

        [Theory]
        // Events: only Organisers manage them
        [InlineData(typeof(EventsController), nameof(EventsController.CreateEvent), "Organiser")]
        [InlineData(typeof(EventsController), nameof(EventsController.UpdateEvent), "Organiser")]
        [InlineData(typeof(EventsController), nameof(EventsController.DeleteEvent), "Organiser")]
        // Categories: only Organisers define them
        [InlineData(typeof(CategoriesController), nameof(CategoriesController.CreateCategory), "Organiser")]
        [InlineData(typeof(CategoriesController), nameof(CategoriesController.UpdateCategory), "Organiser")]
        [InlineData(typeof(CategoriesController), nameof(CategoriesController.DeleteCategory), "Organiser")]
        // Enrolments: Participants enter, Organisers review
        [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.Enrol), "Participant")]
        [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.GetMyEnrolments), "Participant")]
        [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.Withdraw), "Participant")]
        [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.GetEnrolmentsForEvent), "Organiser")]
        // Results: Organisers capture, Participants view their own
        [InlineData(typeof(ResultsController), nameof(ResultsController.CreateResult), "Organiser")]
        [InlineData(typeof(ResultsController), nameof(ResultsController.UpdateResult), "Organiser")]
        [InlineData(typeof(ResultsController), nameof(ResultsController.GetResultsForEvent), "Organiser")]
        [InlineData(typeof(ResultsController), nameof(ResultsController.GetMyResults), "Participant")]
        public void Endpoint_IsRestrictedToExpectedRole(Type controllerType, string methodName, string expectedRole)
        {
            // Arrange
            var method = controllerType.GetMethod(methodName);
            Assert.NotNull(method);

            // Act
            var attribute = method!
                .GetCustomAttributes<SessionAuthorizeAttribute>(inherit: true)
                .FirstOrDefault();

            // Assert
            Assert.NotNull(attribute);
            Assert.Equal(expectedRole, Assert.Single(attribute!.Roles));
        }

        [Theory]
        [InlineData(typeof(ProfileController))]
        [InlineData(typeof(EventsController))]
        [InlineData(typeof(CategoriesController))]
        [InlineData(typeof(EnrolmentsController))]
        [InlineData(typeof(ResultsController))]
        public void Controller_RequiresLogin(Type controllerType)
        {
            // Arrange + Act
            var attribute = controllerType.GetCustomAttribute<SessionAuthorizeAttribute>(inherit: true);

            // Assert
            Assert.NotNull(attribute);
        }
    }
}