using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Api.Tests;

public class EventTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static bool ContainsEvent(JsonElement array, int id) =>
        array.EnumerateArray().Any(e => e.GetProperty("eventId").GetInt32() == id);

    // ---- Organiser can manage events (success) ----

    [Fact]
    public async Task Create_AsOrganiser_Returns201_WithDistanceAndType()
    {
        var org = await Api.LoggedInClient(factory, "Organiser");
        var res = await org.PostAsJsonAsync("/api/events", Api.EventBody(type: "cycle"));
        var json = await Api.Json(res);

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        Assert.Equal("cycle", json.GetProperty("eventType").GetString());
        Assert.Equal(10, json.GetProperty("distanceKm").GetDecimal());
    }

    [Fact]
    public async Task Update_OwnEvent_Returns200()
    {
        var org = await Api.LoggedInClient(factory, "Organiser");
        var id = await Api.CreateEvent(org);

        var res = await org.PutAsJsonAsync($"/api/events/{id}", Api.EventBody(name: "Renamed Event", type: "walk"));
        var json = await Api.Json(res);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("Renamed Event", json.GetProperty("name").GetString());
        Assert.Equal("walk", json.GetProperty("eventType").GetString());
    }

    [Fact]
    public async Task Delete_OwnEvent_Returns204_AndEventIsGone()
    {
        var org = await Api.LoggedInClient(factory, "Organiser");
        var id = await Api.CreateEvent(org);

        var del = await org.DeleteAsync($"/api/events/{id}");
        var get = await factory.CreateClient().GetAsync($"/api/events/{id}");

        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Delete_EventWithEnrolments_MarksItCancelledInsteadOfRemoving()
    {
        var (org, eventId, catId) = await Api.OrganiserWithCategory(factory);
        var runner = await Api.LoggedInClient(factory, "Participant");
        await Api.Enrol(runner, catId);

        var del = await org.DeleteAsync($"/api/events/{eventId}");
        var mine = await Api.Json(await org.GetAsync("/api/events/mine"));
        var ev = mine.EnumerateArray().Single(e => e.GetProperty("eventId").GetInt32() == eventId);

        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
        Assert.Equal("cancelled", ev.GetProperty("status").GetString());
    }

    // ---- Failure scenarios ----

    [Fact]
    public async Task Create_Unauthenticated_Returns401()
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/events", Api.EventBody());
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidEventType_Returns400()
    {
        var org = await Api.LoggedInClient(factory, "Organiser");
        var res = await org.PostAsJsonAsync("/api/events", Api.EventBody(type: "swim"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Create_DateInThePast_Returns400()
    {
        var org = await Api.LoggedInClient(factory, "Organiser");
        var res = await org.PostAsJsonAsync("/api/events", Api.EventBody(date: Api.FutureDate(-5)));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Create_MissingName_Returns400()
    {
        var org = await Api.LoggedInClient(factory, "Organiser");
        var res = await org.PostAsJsonAsync("/api/events", Api.EventBody(name: ""));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Update_NonExistentEvent_Returns404()
    {
        var org = await Api.LoggedInClient(factory, "Organiser");
        var res = await org.PutAsJsonAsync("/api/events/999999", Api.EventBody());
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    // ---- Role enforcement ----

    [Fact]
    public async Task Create_AsParticipant_Returns403()
    {
        var runner = await Api.LoggedInClient(factory, "Participant");
        var res = await runner.PostAsJsonAsync("/api/events", Api.EventBody());
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Update_AsParticipant_Returns403()
    {
        var org = await Api.LoggedInClient(factory, "Organiser");
        var id = await Api.CreateEvent(org);
        var runner = await Api.LoggedInClient(factory, "Participant");

        var res = await runner.PutAsJsonAsync($"/api/events/{id}", Api.EventBody());
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Delete_AsParticipant_Returns403()
    {
        var org = await Api.LoggedInClient(factory, "Organiser");
        var id = await Api.CreateEvent(org);
        var runner = await Api.LoggedInClient(factory, "Participant");

        var res = await runner.DeleteAsync($"/api/events/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Update_EventOwnedByAnotherOrganiser_Returns403()
    {
        var owner = await Api.LoggedInClient(factory, "Organiser");
        var id = await Api.CreateEvent(owner);
        var other = await Api.LoggedInClient(factory, "Organiser");

        var res = await other.PutAsJsonAsync($"/api/events/{id}", Api.EventBody(name: "Hijacked"));
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Delete_EventOwnedByAnotherOrganiser_Returns403()
    {
        var owner = await Api.LoggedInClient(factory, "Organiser");
        var id = await Api.CreateEvent(owner);
        var other = await Api.LoggedInClient(factory, "Organiser");

        var res = await other.DeleteAsync($"/api/events/{id}");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task MyEvents_AsParticipant_Returns403()
    {
        var runner = await Api.LoggedInClient(factory, "Participant");
        var res = await runner.GetAsync("/api/events/mine");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ---- Both roles can view events ----

    [Fact]
    public async Task GetAll_IsPublic_AndOnlyListsPublishedEvents()
    {
        var org = await Api.LoggedInClient(factory, "Organiser");
        var published = await Api.CreateEvent(org, "published");
        var draft = await Api.CreateEvent(org, "draft");

        var res = await factory.CreateClient().GetAsync("/api/events");
        var list = await Api.Json(res);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.True(ContainsEvent(list, published));
        Assert.False(ContainsEvent(list, draft));
    }

    [Fact]
    public async Task GetAll_FilterByEventType_ReturnsOnlyThatType()
    {
        var org = await Api.LoggedInClient(factory, "Organiser");
        await org.PostAsJsonAsync("/api/events", Api.EventBody(type: "cycle"));

        var list = await Api.Json(await factory.CreateClient().GetAsync("/api/events?eventType=cycle"));
        Assert.All(list.EnumerateArray(), e => Assert.Equal("cycle", e.GetProperty("eventType").GetString()));
    }

    [Fact]
    public async Task GetById_Participant_CanViewPublishedEvent_WithCategories()
    {
        var (_, eventId, _) = await Api.OrganiserWithCategory(factory);
        var runner = await Api.LoggedInClient(factory, "Participant");

        var res = await runner.GetAsync($"/api/events/{eventId}");
        var json = await Api.Json(res);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(1, json.GetProperty("categories").GetArrayLength());
    }

    [Fact]
    public async Task GetById_DraftEvent_HiddenFromPublic_ButVisibleToOwner()
    {
        var org = await Api.LoggedInClient(factory, "Organiser");
        var draft = await Api.CreateEvent(org, "draft");

        var publicRes = await factory.CreateClient().GetAsync($"/api/events/{draft}");
        var ownerRes = await org.GetAsync($"/api/events/{draft}");

        Assert.Equal(HttpStatusCode.NotFound, publicRes.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ownerRes.StatusCode);
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404()
    {
        var res = await factory.CreateClient().GetAsync("/api/events/999999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
