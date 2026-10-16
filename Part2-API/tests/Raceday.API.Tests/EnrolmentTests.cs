using System.Net;

namespace RaceDay.Api.Tests;

public class EnrolmentTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    // ---- Participants can enrol, and the enrolment is recorded ----

    [Fact]
    public async Task Enrol_AsParticipant_Returns201_WithBibNumber()
    {
        var (_, _, catId) = await Api.OrganiserWithCategory(factory);
        var runner = await Api.LoggedInClient(factory, "Participant");

        var res = await runner.PostAsync($"/api/categories/{catId}/enrolments", null);
        var json = await Api.Json(res);

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        Assert.Equal(catId, json.GetProperty("categoryId").GetInt32());
        Assert.Equal(1, json.GetProperty("bibNumber").GetInt32());
    }

    [Fact]
    public async Task Enrol_TwoParticipants_GetDifferentBibNumbers()
    {
        var (_, _, catId) = await Api.OrganiserWithCategory(factory);
        var first = await Api.LoggedInClient(factory, "Participant");
        var second = await Api.LoggedInClient(factory, "Participant");

        var bib1 = (await Api.Json(await first.PostAsync($"/api/categories/{catId}/enrolments", null))).GetProperty("bibNumber").GetInt32();
        var bib2 = (await Api.Json(await second.PostAsync($"/api/categories/{catId}/enrolments", null))).GetProperty("bibNumber").GetInt32();

        Assert.NotEqual(bib1, bib2);
    }

    [Fact]
    public async Task Enrolment_IsRecorded_LinksParticipantEventAndCategory()
    {
        var (_, eventId, catId) = await Api.OrganiserWithCategory(factory);
        var email = Api.NewEmail("runner");
        var runner = await Api.LoggedInClient(factory, "Participant", email);
        var enrolmentId = await Api.Enrol(runner, catId);

        var mine = await Api.Json(await runner.GetAsync("/api/users/me/enrolments"));
        var row = mine.EnumerateArray().Single();

        Assert.Equal(enrolmentId, row.GetProperty("enrolmentId").GetInt32());
        Assert.Equal(catId, row.GetProperty("categoryId").GetInt32());
        Assert.Equal(eventId, row.GetProperty("eventId").GetInt32());
        Assert.Equal("Test Participant", row.GetProperty("participantName").GetString());
    }

    [Fact]
    public async Task Organiser_CanViewEnrolmentsForOwnCategory()
    {
        var (org, _, catId) = await Api.OrganiserWithCategory(factory);
        var runner = await Api.LoggedInClient(factory, "Participant");
        var enrolmentId = await Api.Enrol(runner, catId);

        var res = await org.GetAsync($"/api/categories/{catId}/enrolments");
        var list = await Api.Json(res);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(enrolmentId, list.EnumerateArray().Single().GetProperty("enrolmentId").GetInt32());
    }

    [Fact]
    public async Task Withdraw_OwnEnrolment_Returns204_AndItDisappears()
    {
        var (_, _, catId) = await Api.OrganiserWithCategory(factory);
        var runner = await Api.LoggedInClient(factory, "Participant");
        var enrolmentId = await Api.Enrol(runner, catId);

        var del = await runner.DeleteAsync($"/api/enrolments/{enrolmentId}");
        var mine = await Api.Json(await runner.GetAsync("/api/users/me/enrolments"));

        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
        Assert.Equal(0, mine.GetArrayLength());
    }

    // ---- Failure scenarios ----

    [Fact]
    public async Task Enrol_Twice_InSameCategory_Returns409()
    {
        var (_, _, catId) = await Api.OrganiserWithCategory(factory);
        var runner = await Api.LoggedInClient(factory, "Participant");
        await Api.Enrol(runner, catId);

        var res = await runner.PostAsync($"/api/categories/{catId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Enrol_WhenCategoryIsFull_Returns409()
    {
        var (_, _, catId) = await Api.OrganiserWithCategory(factory, max: 1);
        await Api.Enrol(await Api.LoggedInClient(factory, "Participant"), catId);

        var late = await Api.LoggedInClient(factory, "Participant");
        var res = await late.PostAsync($"/api/categories/{catId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Enrol_InNonExistentCategory_Returns404()
    {
        var runner = await Api.LoggedInClient(factory, "Participant");
        var res = await runner.PostAsync("/api/categories/999999/enrolments", null);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Enrol_InDraftEvent_Returns400()
    {
        var org = await Api.LoggedInClient(factory, "Organiser");
        var eventId = await Api.CreateEvent(org, "draft");
        var catId = await Api.CreateCategory(org, eventId);
        var runner = await Api.LoggedInClient(factory, "Participant");

        var res = await runner.PostAsync($"/api/categories/{catId}/enrolments", null);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Enrol_Unauthenticated_Returns401()
    {
        var res = await factory.CreateClient().PostAsync("/api/categories/1/enrolments", null);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ---- Role enforcement ----

    [Fact]
    public async Task Enrol_AsOrganiser_Returns403()
    {
        var (org, _, catId) = await Api.OrganiserWithCategory(factory);
        var res = await org.PostAsync($"/api/categories/{catId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task MyEnrolments_AsOrganiser_Returns403()
    {
        var org = await Api.LoggedInClient(factory, "Organiser");
        var res = await org.GetAsync("/api/users/me/enrolments");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task ViewCategoryEnrolments_AsParticipant_Returns403()
    {
        var (_, _, catId) = await Api.OrganiserWithCategory(factory);
        var runner = await Api.LoggedInClient(factory, "Participant");
        var res = await runner.GetAsync($"/api/categories/{catId}/enrolments");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task ViewCategoryEnrolments_AsOtherOrganiser_Returns403()
    {
        var (_, _, catId) = await Api.OrganiserWithCategory(factory);
        var other = await Api.LoggedInClient(factory, "Organiser");
        var res = await other.GetAsync($"/api/categories/{catId}/enrolments");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Withdraw_SomeoneElsesEnrolment_Returns403()
    {
        var (_, _, catId) = await Api.OrganiserWithCategory(factory);
        var owner = await Api.LoggedInClient(factory, "Participant");
        var enrolmentId = await Api.Enrol(owner, catId);
        var other = await Api.LoggedInClient(factory, "Participant");

        var res = await other.DeleteAsync($"/api/enrolments/{enrolmentId}");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
