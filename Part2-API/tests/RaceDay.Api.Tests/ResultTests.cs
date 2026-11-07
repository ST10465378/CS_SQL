using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Api.Tests;

public class ResultTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly object Body = new { finishTime = "00:28:45", position = 1 };

    private async Task<(HttpClient Org, HttpClient Runner, int CategoryId, int EnrolmentId)> EnrolledRunner()
    {
        var (org, _, catId) = await Api.OrganiserWithCategory(factory);
        var runner = await Api.LoggedInClient(factory, "Participant");
        var enrolmentId = await Api.Enrol(runner, catId);
        return (org, runner, catId, enrolmentId);
    }

    [Fact]
    public async Task Capture_AsOwningOrganiser_Returns201()
    {
        var (org, _, _, enrolmentId) = await EnrolledRunner();
        var res = await org.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", Body);
        var json = await Api.Json(res);

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        Assert.Equal("00:28:45", json.GetProperty("finishTime").GetString());
        Assert.Equal(1, json.GetProperty("position").GetInt32());
    }

    [Fact]
    public async Task Capture_Twice_Returns409()
    {
        var (org, _, _, enrolmentId) = await EnrolledRunner();
        await org.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", Body);
        var res = await org.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", Body);
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Capture_ZeroFinishTime_Returns400()
    {
        var (org, _, _, enrolmentId) = await EnrolledRunner();
        var res = await org.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", new { finishTime = "00:00:00", position = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Capture_NonExistentEnrolment_Returns404()
    {
        var org = await Api.LoggedInClient(factory, "Organiser");
        var res = await org.PostAsJsonAsync("/api/enrolments/999999/results", Body);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Capture_AsParticipant_Returns403()
    {
        var (_, runner, _, enrolmentId) = await EnrolledRunner();
        var res = await runner.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", Body);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Capture_AsOtherOrganiser_Returns403()
    {
        var (_, _, _, enrolmentId) = await EnrolledRunner();
        var other = await Api.LoggedInClient(factory, "Organiser");
        var res = await other.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", Body);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Capture_Unauthenticated_Returns401()
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/enrolments/1/results", Body);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Participant_CanViewOwnResult()
    {
        var (org, runner, _, enrolmentId) = await EnrolledRunner();
        await org.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", Body);

        var res = await runner.GetAsync($"/api/enrolments/{enrolmentId}/results");
        var json = await Api.Json(res);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(1, json.GetProperty("position").GetInt32());
    }

    [Fact]
    public async Task Participant_CannotViewSomeoneElsesResult()
    {
        var (org, _, _, enrolmentId) = await EnrolledRunner();
        await org.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", Body);
        var other = await Api.LoggedInClient(factory, "Participant");

        var res = await other.GetAsync($"/api/enrolments/{enrolmentId}/results");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task ViewResult_BeforeCapture_Returns404()
    {
        var (_, runner, _, enrolmentId) = await EnrolledRunner();
        var res = await runner.GetAsync($"/api/enrolments/{enrolmentId}/results");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task ViewResult_Unauthenticated_Returns401()
    {
        var res = await factory.CreateClient().GetAsync("/api/enrolments/1/results");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Leaderboard_IsPublic_AndOrderedByPosition()
    {
        var (org, _, catId) = await Api.OrganiserWithCategory(factory);
        var runnerA = await Api.LoggedInClient(factory, "Participant");
        var runnerB = await Api.LoggedInClient(factory, "Participant");
        var enrolA = await Api.Enrol(runnerA, catId);
        var enrolB = await Api.Enrol(runnerB, catId);

        await org.PostAsJsonAsync($"/api/enrolments/{enrolA}/results", new { finishTime = "00:40:00", position = 2 });
        await org.PostAsJsonAsync($"/api/enrolments/{enrolB}/results", new { finishTime = "00:35:00", position = 1 });

        var res = await factory.CreateClient().GetAsync($"/api/categories/{catId}/results");
        var list = await Api.Json(res);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(enrolB, list[0].GetProperty("enrolmentId").GetInt32());
        Assert.Equal(enrolA, list[1].GetProperty("enrolmentId").GetInt32());
    }

    [Fact]
    public async Task Capture_DuplicatePositionInCategory_Returns409()
    {
        var (org, _, catId) = await Api.OrganiserWithCategory(factory);
        var enrolA = await Api.Enrol(await Api.LoggedInClient(factory, "Participant"), catId);
        var enrolB = await Api.Enrol(await Api.LoggedInClient(factory, "Participant"), catId);

        await org.PostAsJsonAsync($"/api/enrolments/{enrolA}/results", Body);
        var res = await org.PostAsJsonAsync($"/api/enrolments/{enrolB}/results", new { finishTime = "00:50:00", position = 1 });
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Leaderboard_NonExistentCategory_Returns404()
    {
        var res = await factory.CreateClient().GetAsync("/api/categories/999999/results");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
