using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected int CurrentUserId => HttpContext.Session.GetUserId() ?? 0;
    protected string? CurrentRole => HttpContext.Session.GetRole();
    /// <summary>OrganiserId (Organisers) or ParticipantId (Participants) of the logged-in user.</summary>
    protected int CurrentProfileId => HttpContext.Session.GetProfileId() ?? 0;

    protected Task LoadSessionAsync() => HttpContext.Session.LoadAsync();

    protected ObjectResult Fail(int status, string detail) => Problem(detail: detail, statusCode: status);
}
