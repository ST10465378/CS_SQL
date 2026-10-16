using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Tags("Results")]
public class ResultsController(RaceDayDbContext db) : ApiControllerBase
{
    /// <summary>Capture the finish time and position for an enrolment (Organiser who owns the event).</summary>
    /// <response code="201">Result captured.</response>
    /// <response code="400">Invalid finish time.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of the event.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">Result already captured, or position already taken in this category.</response>
    [HttpPost("api/enrolments/{enrolmentId:int}/results")]
    [SessionAuthorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Capture(int enrolmentId, ResultRequest req)
    {
        var en = await db.Enrolments.Include(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);
        if (en is null) return Fail(404, "Enrolment not found.");
        if (en.Category.Event.OrganiserId != CurrentProfileId) return Fail(403, "You can only capture results for your own events.");
        if (req.FinishTime <= TimeSpan.Zero || req.FinishTime >= TimeSpan.FromHours(24))
            return Fail(400, "FinishTime must be greater than 00:00:00 and less than 24 hours.");
        if (await db.Results.AnyAsync(r => r.EnrolmentId == enrolmentId)) return Fail(409, "A result has already been captured for this enrolment.");
        if (req.Position.HasValue && await db.Results.AnyAsync(r => r.Enrolment.CategoryId == en.CategoryId && r.Position == req.Position))
            return Fail(409, "That position has already been assigned in this category.");

        var result = new Result { EnrolmentId = enrolmentId, FinishTime = req.FinishTime, Position = req.Position };
        db.Results.Add(result);
        await db.SaveChangesAsync();

        var dto = await db.Results.AsNoTracking().Where(r => r.ResultId == result.ResultId).Select(Projections.ResultDto).FirstAsync();
        return Created($"/api/enrolments/{enrolmentId}/results", dto);
    }

    /// <summary>Get the leaderboard for a category, ordered by position (public).</summary>
    /// <response code="200">Array of results.</response>
    /// <response code="404">Category not found.</response>
    [HttpGet("api/categories/{categoryId:int}/results")]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Leaderboard(int categoryId)
    {
        if (!await db.Categories.AnyAsync(c => c.CategoryId == categoryId)) return Fail(404, "Category not found.");
        return Ok(await db.Results.AsNoTracking().Where(r => r.Enrolment.CategoryId == categoryId)
            .OrderBy(r => r.Position == null).ThenBy(r => r.Position).ThenBy(r => r.FinishTime)
            .Select(Projections.ResultDto).ToListAsync());
    }

    /// <summary>Get the result for one enrolment (the Participant who owns it, or the Organiser who owns the event).</summary>
    /// <response code="200">The result.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not the owner of the enrolment or its event.</response>
    /// <response code="404">Enrolment not found, or no result captured yet.</response>
    [HttpGet("api/enrolments/{enrolmentId:int}/results")]
    [SessionAuthorize]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ForEnrolment(int enrolmentId)
    {
        var en = await db.Enrolments.AsNoTracking().Include(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);
        if (en is null) return Fail(404, "Enrolment not found.");

        var allowed = CurrentRole == Roles.Participant
            ? en.ParticipantId == CurrentProfileId
            : en.Category.Event.OrganiserId == CurrentProfileId;
        if (!allowed) return Fail(403, "You can only view your own results.");

        var dto = await db.Results.AsNoTracking().Where(r => r.EnrolmentId == enrolmentId).Select(Projections.ResultDto).FirstOrDefaultAsync();
        return dto is null ? Fail(404, "No result has been captured for this enrolment yet.") : Ok(dto);
    }
}
