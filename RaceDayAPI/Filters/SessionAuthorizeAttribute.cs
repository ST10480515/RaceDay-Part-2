using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDayAPI.Filters
{
    /// <summary>
    /// Requires a logged-in session. Optionally restricts access to the given roles,
    /// e.g. [SessionAuthorize("Organiser")].
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _roles;

        public SessionAuthorizeAttribute(params string[] roles)
        {
            _roles = roles;
        }
        public IReadOnlyList<string> Roles => _roles;

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var session = context.HttpContext.Session;
            var userId = session.GetInt32("UserID");
            var role = session.GetString("Role");

            // Not logged in -> 401
            if (userId == null || role == null)
            {
                context.Result = new UnauthorizedObjectResult("You must be logged in.");
                return;
            }

            // Logged in but wrong role -> 403
            if (_roles.Length > 0 && !_roles.Contains(role))
            {
                context.Result = new ObjectResult("You do not have permission to access this resource.")
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }
}