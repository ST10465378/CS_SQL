using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Api.Tests;

public class CategoryTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly object Body = new { name = "Under 20", distanceKm = 5, maxParticipants = 50, entryFee = 80 };

    [Fact]
    public async Task Create_AsOwningOrganiser_Returns201()
    {
        var org = await Api.LoggedInClient(factory, "Organiser");
        var eventId = await Api.CreateEvent(org);
        var res = await org.PostAsJsonAsync($"/api/events/{eventId}/categories", Body);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Create_AsParticipant_Returns403()
    {
        var org = await Api.LoggedInClient(factory, "Organiser");
        var eventId = await Api.CreateEvent(org);
        var runner = await Api.LoggedInClient(factory, "Participant");

        var res = await runner.PostAsJsonAsync($"/api/events/{eventId}/categories", Body);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Create_OnAnotherOrganisersEvent_Returns403()
    {
        var owner = await Api.LoggedInClient(factory, "Organiser");
        var eventId = await Api.CreateEvent(owner);
        var other = await Api.LoggedInClient(factory, "Organiser");

        var res = await other.PostAsJsonAsync($"/api/events/{eventId}/categories", Body);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Create_Unauthenticated_Returns401()
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/events/1/categories", Body);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Create_OnMissingEvent_Returns404()
    {
        var org = await Api.LoggedInClient(factory, "Organiser");
        var res = await org.PostAsJsonAsync("/api/events/999999/categories", Body);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task List_IsPublic_AndShowsCategories()
    {
        var (_, eventId, _) = await Api.OrganiserWithCategory(factory);
        var res = await factory.CreateClient().GetAsync($"/api/events/{eventId}/categories");
        var list = await Api.Json(res);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(1, list.GetArrayLength());
    }

    [Fact]
    public async Task Update_OwnCategory_Returns200()
    {
        var (org, _, catId) = await Api.OrganiserWithCategory(factory);
        var res = await org.PutAsJsonAsync($"/api/categories/{catId}", new { name = "Senior", distanceKm = 21, maxParticipants = 10, entryFee = 200 });
        var json = await Api.Json(res);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("Senior", json.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Delete_CategoryWithEnrolments_Returns409()
    {
        var (org, _, catId) = await Api.OrganiserWithCategory(factory);
        var runner = await Api.LoggedInClient(factory, "Participant");
        await Api.Enrol(runner, catId);

        var res = await org.DeleteAsync($"/api/categories/{catId}");
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Delete_EmptyCategory_Returns204()
    {
        var (org, _, catId) = await Api.OrganiserWithCategory(factory);
        var res = await org.DeleteAsync($"/api/categories/{catId}");
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }
}
