/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
*/
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDayAPI.Controllers;
using RaceDayAPI.Models.Dtos;
using Xunit;

namespace RaceDayAPI.Tests
{
    public class AuthControllerTests
    {
        // ---------- Registration ----------

        [Fact]
        public async Task Register_ValidOrganiser_ReturnsCreated()
        {
            // Arrange
            using var db = TestHelpers.CreateContext();
            var controller = new AuthController(db).WithSession();
            var dto = new RegisterDto
            {
                FullName = "Test Organiser",
                Email = "organiser@test.com",
                Password = "Password123",
                Role = "Organiser"
            };

            // Act
            var result = await controller.Register(dto);

            // Assert
            var created = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
            Assert.Equal("Organiser", db.Users.Single().Role);
        }

        [Fact]
        public async Task Register_ValidParticipant_ReturnsCreated()
        {
            // Arrange
            using var db = TestHelpers.CreateContext();
            var controller = new AuthController(db).WithSession();
            var dto = new RegisterDto
            {
                FullName = "Test Participant",
                Email = "participant@test.com",
                Password = "Password123",
                Role = "Participant"
            };

            // Act
            var result = await controller.Register(dto);

            // Assert
            var created = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
            Assert.Equal("Participant", db.Users.Single().Role);
        }

        [Fact]
        public async Task Register_ValidUser_StoresHashedPasswordNotPlainText()
        {
            // Arrange
            using var db = TestHelpers.CreateContext();
            var controller = new AuthController(db).WithSession();
            var dto = new RegisterDto
            {
                FullName = "Test User",
                Email = "hash@test.com",
                Password = "Password123",
                Role = "Participant"
            };

            // Act
            await controller.Register(dto);

            // Assert
            var saved = db.Users.Single();
            Assert.NotEqual("Password123", saved.PasswordHash);
            Assert.True(BCrypt.Net.BCrypt.Verify("Password123", saved.PasswordHash));
        }

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsConflict()
        {
            // Arrange
            using var db = TestHelpers.CreateContext();
            var existing = TestHelpers.AddUser(db, "Participant");
            var controller = new AuthController(db).WithSession();
            var dto = new RegisterDto
            {
                FullName = "Another Person",
                Email = existing.Email,
                Password = "Password123",
                Role = "Participant"
            };

            // Act
            var result = await controller.Register(dto);

            // Assert
            Assert.IsType<ConflictObjectResult>(result);
            Assert.Single(db.Users);
        }

        [Fact]
        public async Task Register_InvalidRole_ReturnsBadRequest()
        {
            // Arrange
            using var db = TestHelpers.CreateContext();
            var controller = new AuthController(db).WithSession();
            var dto = new RegisterDto
            {
                FullName = "Test User",
                Email = "admin@test.com",
                Password = "Password123",
                Role = "Admin"
            };

            // Act
            var result = await controller.Register(dto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Empty(db.Users);
        }

        // ---------- Login ----------

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOk()
        {
            // Arrange
            using var db = TestHelpers.CreateContext();
            var user = TestHelpers.AddUser(db, "Organiser");
            var controller = new AuthController(db).WithSession();
            var dto = new LoginDto { Email = user.Email, Password = TestHelpers.TestPassword };

            // Act
            var result = await controller.Login(dto);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Login_ValidCredentials_StartsSessionWithUserIdAndRole()
        {
            // Arrange
            using var db = TestHelpers.CreateContext();
            var user = TestHelpers.AddUser(db, "Participant");
            var controller = new AuthController(db).WithSession();
            var dto = new LoginDto { Email = user.Email, Password = TestHelpers.TestPassword };

            // Act
            await controller.Login(dto);

            // Assert
            var session = controller.HttpContext.Session;
            Assert.Equal(user.UserID, session.GetInt32("UserID"));
            Assert.Equal("Participant", session.GetString("Role"));
        }

        [Fact]
        public async Task Login_WrongPassword_ReturnsUnauthorized()
        {
            // Arrange
            using var db = TestHelpers.CreateContext();
            var user = TestHelpers.AddUser(db, "Participant");
            var controller = new AuthController(db).WithSession();
            var dto = new LoginDto { Email = user.Email, Password = "WrongPassword" };

            // Act
            var result = await controller.Login(dto);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Null(controller.HttpContext.Session.GetInt32("UserID"));
        }

        [Fact]
        public async Task Login_UnknownEmail_ReturnsUnauthorized()
        {
            // Arrange
            using var db = TestHelpers.CreateContext();
            var controller = new AuthController(db).WithSession();
            var dto = new LoginDto { Email = "nobody@test.com", Password = "Password123" };

            // Act
            var result = await controller.Login(dto);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        // ---------- Session: me and logout ----------

        [Fact]
        public async Task Me_NotLoggedIn_ReturnsUnauthorized()
        {
            // Arrange
            using var db = TestHelpers.CreateContext();
            var controller = new AuthController(db).WithSession();

            // Act
            var result = await controller.Me();

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Me_LoggedIn_ReturnsOk()
        {
            // Arrange
            using var db = TestHelpers.CreateContext();
            var user = TestHelpers.AddUser(db, "Organiser");
            var controller = new AuthController(db).WithSession(user.UserID, "Organiser");

            // Act
            var result = await controller.Me();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<UserResponseDto>(ok.Value);
            Assert.Equal(user.Email, body.Email);
        }

        [Fact]
        public void Logout_LoggedInUser_ClearsSession()
        {
            // Arrange
            using var db = TestHelpers.CreateContext();
            var user = TestHelpers.AddUser(db, "Participant");
            var controller = new AuthController(db).WithSession(user.UserID, "Participant");

            // Act
            var result = controller.Logout();

            // Assert
            Assert.IsType<OkObjectResult>(result);
            Assert.Null(controller.HttpContext.Session.GetInt32("UserID"));
            Assert.Null(controller.HttpContext.Session.GetString("Role"));
        }
    }

    /// <summary>
    /// [ApiController] rejects invalid input before the action runs, so we test
    /// the validation rules on the DTO itself.
    /// </summary>
    public class RegisterDtoValidationTests
    {
        [Theory]
        [InlineData("Test User", "user@test.com", "Password123", "Participant", true)]
        [InlineData("Test User", "", "Password123", "Participant", false)]
        [InlineData("Test User", "not-an-email", "Password123", "Participant", false)]
        [InlineData("Test User", "user@test.com", "short", "Participant", false)]
        [InlineData("Test User", "user@test.com", "", "Participant", false)]
        [InlineData("", "user@test.com", "Password123", "Participant", false)]
        [InlineData("Test User", "user@test.com", "Password123", "", false)]
        public void RegisterDto_Validation_MatchesExpectedResult(
            string fullName, string email, string password, string role, bool expectedValid)
        {
            // Arrange
            var dto = new RegisterDto
            {
                FullName = fullName,
                Email = email,
                Password = password,
                Role = role
            };
            var results = new List<ValidationResult>();

            // Act
            var isValid = Validator.TryValidateObject(
                dto, new ValidationContext(dto), results, validateAllProperties: true);

            // Assert
            Assert.Equal(expectedValid, isValid);
        }
    }
}
