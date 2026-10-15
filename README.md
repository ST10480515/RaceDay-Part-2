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
