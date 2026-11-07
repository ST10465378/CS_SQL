using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api/auth")]
[Tags("Authentication")]
public class AuthController(RaceDayDbContext db, IConfiguration config) : ApiControllerBase
{
    /// <summary>Register a new Organiser or Participant account.</summary>
    /// <remarks>
    /// Organisers must supply <c>organisationName</c>; Participants must supply <c>dateOfBirth</c>.
    /// If the server has a <c>Registration:OrganiserCode</c> configured, Organisers must also supply it,
    /// so that not just anyone can self-assign the Organiser role.
    /// </remarks>
    /// <response code="201">Account created. Returns the user (never the password).</response>
    /// <response code="400">Validation error.</response>
    /// <response code="403">Organiser registration code missing or wrong.</response>
    /// <response code="409">Email already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        var role = req.Role.Trim().ToLowerInvariant() switch
        {
            "organiser" => Roles.Organiser,
            "participant" => Roles.Participant,
            _ => null
        };
        if (role is null) return Fail(400, "Role must be either 'Organiser' or 'Participant'.");

        var user = new User
        {
            FullName = req.FullName.Trim(),
            Email = req.Email.Trim().ToLowerInvariant(),
            PasswordHash = PasswordService.Hash(req.Password),
            Role = role
        };

        if (role == Roles.Organiser)
        {
            if (string.IsNullOrWhiteSpace(req.OrganisationName))
                return Fail(400, "OrganisationName is required for Organisers.");
            var required = config["Registration:OrganiserCode"];
            if (!string.IsNullOrEmpty(required) && req.OrganiserCode != required)
                return Fail(403, "A valid organiser registration code is required.");
            user.Organiser = new Organiser { OrganisationName = req.OrganisationName.Trim(), ContactPhone = req.ContactPhone };
        }
        else
        {
            if (req.DateOfBirth is null || req.DateOfBirth.Value >= DateOnly.FromDateTime(DateTime.Today))
                return Fail(400, "A valid DateOfBirth in the past is required for Participants.");
            user.Participant = new Participant { DateOfBirth = req.DateOfBirth.Value, EmergencyContact = req.EmergencyContact };
        }

        if (await db.Users.AnyAsync(u => u.Email == user.Email))
            return Fail(409, "That email address is already registered.");

        db.Users.Add(user);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { return Fail(409, "That email address is already registered."); }

        return StatusCode(StatusCodes.Status201Created, user.ToResponse());
    }

    /// <summary>Log in with email and password.</summary>
    /// <remarks>On success a server-side session is created holding the user's ID and role; the session cookie is sent automatically on later requests (including from Swagger UI).</remarks>
    /// <response code="200">Logged in. Returns the user.</response>
    /// <response code="401">Invalid credentials.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var user = await db.Users.Include(u => u.Organiser).Include(u => u.Participant)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user is null || !PasswordService.Verify(req.Password, user.PasswordHash))
            return Fail(401, "Invalid email or password.");

        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserId);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role);
        HttpContext.Session.SetInt32(SessionKeys.ProfileId,
            user.Role == Roles.Organiser ? user.Organiser!.OrganiserId : user.Participant!.ParticipantId);
        await HttpContext.Session.CommitAsync();

        return Ok(new LoginResponse("Logged in.", user.ToResponse()));
    }

    /// <summary>Log out and destroy the current session.</summary>
    /// <response code="204">Session ended.</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return NoContent();
    }
}
