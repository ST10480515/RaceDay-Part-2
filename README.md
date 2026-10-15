# RaceDay-Part-2
1. Project skeleton
git add RaceDayAPI.sln RaceDayAPI/RaceDayAPI.csproj RaceDayAPI/Program.cs RaceDayAPI/appsettings.json RaceDayAPI/appsettings.Development.json RaceDayAPI/Properties RaceDayAPI/RaceDayAPI.http
git commit -m "Create ASP.NET Core Web API project with EF Core, session and Swagger setup"
# 2. User model
git add RaceDayAPI/Models/Users.cs RaceDayAPI/Models/UserRole.cs
git commit -m "Add Users model with hashed password and role"
# 3. Events and categories models
git add RaceDayAPI/Models/Event.cs RaceDayAPI/Models/Category.cs
git commit -m "Add Event and Category models"
 # 4. Enrolment, payment and result models
git add RaceDayAPI/Models/EventEnrolment.cs RaceDayAPI/Models/Payment.cs RaceDayAPI/Models/Result.cs
git commit -m "Add EventEnrolment, Payment and Result models"
  # 5. DbContext
git add RaceDayAPI/Data
git commit -m "Add RaceDayDbContext with relationships, defaults and check constraints"  
# 6. Migration
git add RaceDayAPI/Migrations
git commit -m "Add initial EF Core migration for the RaceDay schema"
# 7. Auth DTOs
git add RaceDayAPI/Models/Dtos/RegisterDto.cs RaceDayAPI/Models/Dtos/LoginDto.cs
git commit -m "Add registration and login DTOs with validation"
# 8. Auth controller
git add RaceDayAPI/Controllers/AuthController.cs
git commit -m "Add AuthController: register with BCrypt hashing, login, logout and me"
# 9. Role filter
git add RaceDayAPI/Filters
git commit -m "Add SessionAuthorize filter for login and role-based access"
# 10. Events
git add RaceDayAPI/Models/Dtos/EventDtos.cs
git commit -m "Add event request and response DTOs"
git add RaceDayAPI/Controllers/EventsController.cs
git commit -m "Add EventsController with Organiser-only create, update and delete"
# 11. Categories
git add RaceDayAPI/Models/Dtos/CategoryDtos.cs
git commit -m "Add category DTOs"
git add RaceDayAPI/Controllers/CategoriesController.cs
git commit -m "Add CategoriesController for Organiser-managed event categories"
