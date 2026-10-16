using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api/events")]
[Tags("Events")]
public class EventsController(RaceDayDbContext db) : ApiControllerBase
{
    private static string? NormaliseType(string t) =>
        EventTypes.All.FirstOrDefault(x => x == t.Trim().ToLowerInvariant());

    private static string? NormaliseStatus(string? s) =>
        string.IsNullOrWhiteSpace(s) ? EventStatuses.Draft : EventStatuses.All.FirstOrDefault(x => x == s.Trim().ToLowerInvariant());

    /// <summary>List all published events (public). Optional filters: search text, event type, and earliest date.</summary>
    /// <response code="200">Array of published events (without categories).</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? eventType, [FromQuery] DateOnly? from)
    {
        var q = db.Events.AsNoTracking().Where(e => e.Status == EventStatuses.Published);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(e => e.Name.Contains(search) || e.Location.Contains(search));
        if (!string.IsNullOrWhiteSpace(eventType)) { var t = eventType.Trim().ToLowerInvariant(); q = q.Where(e => e.EventType == t); }
        if (from.HasValue) q = q.Where(e => e.EventDate >= from.Value);
        return Ok(await q.OrderBy(e => e.EventDate).Select(Projections.EventDto).ToListAsync());
    }

    /// <summary>List all events (any status) owned by the logged-in Organiser.</summary>
    /// <response code="200">Array of the organiser's events.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser.</response>
    [HttpGet("mine")]
    [SessionAuthorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var oid = CurrentProfileId;
        return Ok(await db.Events.AsNoTracking().Where(e => e.OrganiserId == oid)
            .OrderBy(e => e.EventDate).Select(Projections.EventDto).ToListAsync());
    }

    /// <summary>Get one event with its categories (public for published events; drafts only visible to the owning Organiser).</summary>
    /// <response code="200">The event including categories.</response>
    /// <response code="404">Event does not exist (or is an unpublished event you do not own).</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        await LoadSessionAsync();
        var dto = await db.Events.AsNoTracking().Where(e => e.EventId == id).Select(Projections.EventDto).FirstOrDefaultAsync();
        if (dto is null) return Fail(404, "Event not found.");

        if (dto.Status != EventStatuses.Published && !(CurrentRole == Roles.Organiser && dto.OrganiserId == CurrentProfileId))
            return Fail(404, "Event not found.");

        dto.Categories = await db.Categories.AsNoTracking().Where(c => c.EventId == id).Select(Projections.CategoryDto).ToListAsync();
        return Ok(dto);
    }

    /// <summary>Create an event owned by the logged-in Organiser.</summary>
    /// <response code="201">Event created.</response>
    /// <response code="400">Validation error (bad type/status, date in the past, etc.).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser.</response>
    [HttpPost]
    [SessionAuthorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(EventRequest req)
    {
        var type = NormaliseType(req.EventType);
        if (type is null) return Fail(400, "EventType must be one of: run, walk, cycle.");
        var status = NormaliseStatus(req.Status);
        if (status is null) return Fail(400, "Status must be one of: draft, published, cancelled.");
        if (req.EventDate < DateOnly.FromDateTime(DateTime.Today)) return Fail(400, "EventDate cannot be in the past.");

        var ev = new Event
        {
            OrganiserId = CurrentProfileId, Name = req.Name.Trim(), EventDate = req.EventDate, Location = req.Location.Trim(),
            Description = req.Description, DistanceKm = req.DistanceKm, EventType = type, Status = status
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var dto = await db.Events.AsNoTracking().Where(e => e.EventId == ev.EventId).Select(Projections.EventDto).FirstAsync();
        return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, dto);
    }

    /// <summary>Update an event. Only the owning Organiser may do this.</summary>
    /// <response code="200">Updated event.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPut("{id:int}")]
    [SessionAuthorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, EventRequest req)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return Fail(404, "Event not found.");
        if (ev.OrganiserId != CurrentProfileId) return Fail(403, "You can only edit your own events.");

        var type = NormaliseType(req.EventType);
        if (type is null) return Fail(400, "EventType must be one of: run, walk, cycle.");
        var status = NormaliseStatus(req.Status ?? ev.Status);
        if (status is null) return Fail(400, "Status must be one of: draft, published, cancelled.");

        ev.Name = req.Name.Trim(); ev.EventDate = req.EventDate; ev.Location = req.Location.Trim();
        ev.Description = req.Description; ev.DistanceKm = req.DistanceKm; ev.EventType = type; ev.Status = status;
        await db.SaveChangesAsync();

        return Ok(await db.Events.AsNoTracking().Where(e => e.EventId == id).Select(Projections.EventDto).FirstAsync());
    }

    /// <summary>Delete an event. If it already has enrolments it is marked "cancelled" instead of being removed.</summary>
    /// <response code="204">Event deleted or cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpDelete("{id:int}")]
    [SessionAuthorize(Roles = Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return Fail(404, "Event not found.");
        if (ev.OrganiserId != CurrentProfileId) return Fail(403, "You can only delete your own events.");

        if (await db.Enrolments.AnyAsync(e => e.Category.EventId == id)) ev.Status = EventStatuses.Cancelled;
        else db.Events.Remove(ev);

        await db.SaveChangesAsync();
        return NoContent();
    }
}
