using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api/users/me")]
[Tags("User Profile")]
[SessionAuthorize]
public class UsersController(RaceDayDbContext db) : ApiControllerBase
{
    private Task<User?> LoadUser() => db.Users.Include(u => u.Organiser).Include(u => u.Participant)
        .FirstOrDefaultAsync(u => u.UserId == CurrentUserId);

    /// <summary>Get the profile of the logged-in user (Organiser or Participant).</summary>
    /// <response code="200">The user's profile.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me()
    {
        var user = await LoadUser();
        if (user is null) { HttpContext.Session.Clear(); return Fail(401, "Session is no longer valid."); }
        return Ok(user.ToResponse());
    }

    /// <summary>Update the logged-in user's own profile.</summary>
    /// <remarks>Organisers may change organisationName/contactPhone; Participants may change dateOfBirth/emergencyContact. Fields that do not apply to your role are ignored.</remarks>
    /// <response code="200">Updated profile.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPut]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(UpdateProfileRequest req)
    {
        var user = await LoadUser();
        if (user is null) { HttpContext.Session.Clear(); return Fail(401, "Session is no longer valid."); }

        user.FullName = req.FullName.Trim();
        if (user.Role == Roles.Organiser)
        {
            if (!string.IsNullOrWhiteSpace(req.OrganisationName)) user.Organiser!.OrganisationName = req.OrganisationName.Trim();
            if (req.ContactPhone is not null) user.Organiser!.ContactPhone = req.ContactPhone;
        }
        else
        {
            if (req.DateOfBirth.HasValue)
            {
                if (req.DateOfBirth.Value >= DateOnly.FromDateTime(DateTime.Today))
                    return Fail(400, "DateOfBirth must be in the past.");
                user.Participant!.DateOfBirth = req.DateOfBirth.Value;
            }
            if (req.EmergencyContact is not null) user.Participant!.EmergencyContact = req.EmergencyContact;
        }

        await db.SaveChangesAsync();
        return Ok(user.ToResponse());
    }

    /// <summary>Delete the logged-in user's account and end the session.</summary>
    /// <response code="204">Account deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="409">Organiser still has events with enrolments; cancel those events instead.</response>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete()
    {
        var user = await LoadUser();
        if (user is null) { HttpContext.Session.Clear(); return Fail(401, "Session is no longer valid."); }

        if (user.Role == Roles.Organiser && await db.Enrolments.AnyAsync(e => e.Category.Event.OrganiserId == user.Organiser!.OrganiserId))
            return Fail(409, "You have events with enrolled participants. Cancel those events instead of deleting your account.");

        db.Users.Remove(user);
        await db.SaveChangesAsync();
        HttpContext.Session.Clear();
        return NoContent();
    }
}
