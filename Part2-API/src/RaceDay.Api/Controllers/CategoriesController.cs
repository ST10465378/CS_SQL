using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Tags("Categories")]
public class CategoriesController(RaceDayDbContext db) : ApiControllerBase
{
    /// <summary>List the categories of an event (public).</summary>
    /// <response code="200">Array of categories with current enrolment counts.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("api/events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await db.Events.AnyAsync(e => e.EventId == eventId)) return Fail(404, "Event not found.");
        return Ok(await db.Categories.AsNoTracking().Where(c => c.EventId == eventId)
            .OrderBy(c => c.DistanceKm).Select(Projections.CategoryDto).ToListAsync());
    }

    /// <summary>Add a category (e.g. "Under 20", "Senior", "10km", "21km") to an event you own.</summary>
    /// <response code="201">Category created.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of the event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPost("api/events/{eventId:int}/categories")]
    [SessionAuthorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, CategoryRequest req)
    {
        var ev = await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null) return Fail(404, "Event not found.");
        if (ev.OrganiserId != CurrentProfileId) return Fail(403, "You can only add categories to your own events.");

        var cat = new Category
        {
            EventId = eventId, Name = req.Name.Trim(), DistanceKm = req.DistanceKm,
            MaxParticipants = req.MaxParticipants, EntryFee = req.EntryFee
        };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        var dto = await db.Categories.AsNoTracking().Where(c => c.CategoryId == cat.CategoryId).Select(Projections.CategoryDto).FirstAsync();
        return Created($"/api/events/{eventId}/categories", dto);
    }

    /// <summary>Update a category of an event you own.</summary>
    /// <response code="200">Updated category.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner.</response>
    /// <response code="404">Category not found.</response>
    [HttpPut("api/categories/{id:int}")]
    [SessionAuthorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, CategoryRequest req)
    {
        var cat = await db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (cat is null) return Fail(404, "Category not found.");
        if (cat.Event.OrganiserId != CurrentProfileId) return Fail(403, "You can only edit categories of your own events.");

        cat.Name = req.Name.Trim(); cat.DistanceKm = req.DistanceKm;
        cat.MaxParticipants = req.MaxParticipants; cat.EntryFee = req.EntryFee;
        await db.SaveChangesAsync();

        return Ok(await db.Categories.AsNoTracking().Where(c => c.CategoryId == id).Select(Projections.CategoryDto).FirstAsync());
    }

    /// <summary>Delete a category of an event you own.</summary>
    /// <response code="204">Category deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Category has existing enrolments.</response>
    [HttpDelete("api/categories/{id:int}")]
    [SessionAuthorize(Roles = Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var cat = await db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (cat is null) return Fail(404, "Category not found.");
        if (cat.Event.OrganiserId != CurrentProfileId) return Fail(403, "You can only delete categories of your own events.");
        if (await db.Enrolments.AnyAsync(e => e.CategoryId == id)) return Fail(409, "This category has enrolments and cannot be deleted.");

        db.Categories.Remove(cat);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
