using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Tags("Event Enrolments")]
public class EnrolmentsController(RaceDayDbContext db) : ApiControllerBase
{
    /// <summary>Enrol the logged-in Participant into a category. The enrolment links participant, event (via category) and category, and a bib number is assigned.</summary>
    /// <response code="201">Enrolment created, including the bib number.</response>
    /// <response code="400">Event is not published or has already taken place.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Already enrolled, or the category is full.</response>
    [HttpPost("api/categories/{categoryId:int}/enrolments")]
    [SessionAuthorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int categoryId)
    {
        var cat = await db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        if (cat is null) return Fail(404, "Category not found.");
        if (cat.Event.Status != EventStatuses.Published) return Fail(400, "This event is not open for enrolment.");
        if (cat.Event.EventDate < DateOnly.FromDateTime(DateTime.Today)) return Fail(400, "This event has already taken place.");

        var pid = CurrentProfileId;
        if (await db.Enrolments.AnyAsync(e => e.CategoryId == categoryId && e.ParticipantId == pid))
            return Fail(409, "You are already enrolled in this category.");

        var count = await db.Enrolments.CountAsync(e => e.CategoryId == categoryId);
        if (cat.MaxParticipants > 0 && count >= cat.MaxParticipants) return Fail(409, "This category is full.");

        var maxBib = await db.Enrolments.Where(e => e.Category.EventId == cat.EventId).MaxAsync(e => e.BibNumber) ?? 0;
        var enrolment = new Enrolment { ParticipantId = pid, CategoryId = categoryId, BibNumber = maxBib + 1 };
        db.Enrolments.Add(enrolment);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { return Fail(409, "You are already enrolled in this category."); }

        var dto = await db.Enrolments.AsNoTracking().Where(e => e.EnrolmentId == enrolment.EnrolmentId)
            .Select(Projections.EnrolmentDto).FirstAsync();
        return Created("/api/users/me/enrolments", dto);
    }

    /// <summary>List the logged-in Participant's own enrolments.</summary>
    /// <response code="200">Array of enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant.</response>
    [HttpGet("api/users/me/enrolments")]
    [SessionAuthorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Mine()
    {
        var pid = CurrentProfileId;
        return Ok(await db.Enrolments.AsNoTracking().Where(e => e.ParticipantId == pid)
            .OrderBy(e => e.Category.Event.EventDate).Select(Projections.EnrolmentDto).ToListAsync());
    }

    /// <summary>List everyone enrolled in a category of an event you own.</summary>
    /// <response code="200">Array of enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of the event.</response>
    /// <response code="404">Category not found.</response>
    [HttpGet("api/categories/{categoryId:int}/enrolments")]
    [SessionAuthorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ForCategory(int categoryId)
    {
        var cat = await db.Categories.AsNoTracking().Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        if (cat is null) return Fail(404, "Category not found.");
        if (cat.Event.OrganiserId != CurrentProfileId) return Fail(403, "You can only view enrolments for your own events.");

        return Ok(await db.Enrolments.AsNoTracking().Where(e => e.CategoryId == categoryId)
            .OrderBy(e => e.BibNumber).Select(Projections.EnrolmentDto).ToListAsync());
    }

    /// <summary>Withdraw your own enrolment (only before the event takes place).</summary>
    /// <response code="204">Enrolment withdrawn.</response>
    /// <response code="400">The event has already taken place.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant, or not your enrolment.</response>
    /// <response code="404">Enrolment not found.</response>
    [HttpDelete("api/enrolments/{id:int}")]
    [SessionAuthorize(Roles = Roles.Participant)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Withdraw(int id)
    {
        var en = await db.Enrolments.Include(e => e.Category).ThenInclude(c => c.Event).FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (en is null) return Fail(404, "Enrolment not found.");
        if (en.ParticipantId != CurrentProfileId) return Fail(403, "You can only withdraw your own enrolments.");
        if (en.Category.Event.EventDate <= DateOnly.FromDateTime(DateTime.Today))
            return Fail(400, "You cannot withdraw once the event has taken place.");

        db.Enrolments.Remove(en);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
