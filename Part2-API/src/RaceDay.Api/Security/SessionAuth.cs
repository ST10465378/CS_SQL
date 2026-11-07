using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Security;

public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
    /// <summary>OrganiserId for Organisers, ParticipantId for Participants.</summary>
    public const string ProfileId = "ProfileId";
}

public static class RaceDaySessionExtensions
{
    public static int? GetUserId(this ISession s) => s.GetInt32(SessionKeys.UserId);
    public static string? GetRole(this ISession s) => s.GetString(SessionKeys.Role);
    public static int? GetProfileId(this ISession s) => s.GetInt32(SessionKeys.ProfileId);
}

/// <summary>
/// Server-side session authorisation. No session => 401. Wrong role => 403.
/// Usage: [SessionAuthorize] (any logged-in user) or [SessionAuthorize(Roles = Roles.Organiser)].
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    public string? Roles { get; set; }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        await session.LoadAsync();

        if (session.GetUserId() is null)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = 401, Title = "Unauthorized", Detail = "You must log in to access this resource."
            }) { StatusCode = 401 };
            return;
        }

        if (!string.IsNullOrWhiteSpace(Roles))
        {
            var allowed = Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!allowed.Contains(session.GetRole(), StringComparer.OrdinalIgnoreCase))
            {
                context.Result = new ObjectResult(new ProblemDetails
                {
                    Status = 403, Title = "Forbidden",
                    Detail = $"This action requires the role: {string.Join(" or ", allowed)}."
                }) { StatusCode = 403 };
            }
        }
    }
}
