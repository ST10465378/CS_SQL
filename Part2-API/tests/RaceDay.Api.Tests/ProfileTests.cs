using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Api.Tests;

public class ProfileTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task GetMe_Unauthenticated_Returns401()
    {
        var res = await factory.CreateClient().GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task GetMe_AsParticipant_ReturnsOwnProfile()
    {
        var email = Api.NewEmail();
        var c = await Api.LoggedInClient(factory, "Participant", email);
        var json = await Api.Json(await c.GetAsync("/api/users/me"));

        Assert.Equal(email, json.GetProperty("email").GetString());
        Assert.Equal("Participant", json.GetProperty("role").GetString());
    }

    [Fact]
    public async Task GetMe_AsOrganiser_ReturnsOrganisationName()
    {
        var c = await Api.LoggedInClient(factory, "Organiser");
        var json = await Api.Json(await c.GetAsync("/api/users/me"));
        Assert.Equal("Test Club", json.GetProperty("organisationName").GetString());
    }

    [Fact]
    public async Task UpdateMe_ChangesOwnProfile()
    {
        var c = await Api.LoggedInClient(factory, "Participant");
        var res = await c.PutAsJsonAsync("/api/users/me", new { fullName = "Updated Name", emergencyContact = "Dad - 083 111 1111" });
        var json = await Api.Json(res);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("Updated Name", json.GetProperty("fullName").GetString());
        Assert.Equal("Dad - 083 111 1111", json.GetProperty("emergencyContact").GetString());
    }

    [Fact]
    public async Task UpdateMe_Unauthenticated_Returns401()
    {
        var res = await factory.CreateClient().PutAsJsonAsync("/api/users/me", new { fullName = "Nope" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task UpdateMe_MissingFullName_Returns400()
    {
        var c = await Api.LoggedInClient(factory, "Participant");
        var res = await c.PutAsJsonAsync("/api/users/me", new { fullName = "" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task DeleteMe_RemovesAccount_AndLoginNoLongerWorks()
    {
        var email = Api.NewEmail();
        var c = await Api.LoggedInClient(factory, "Participant", email);

        var del = await c.DeleteAsync("/api/users/me");
        var login = await Api.Login(factory.CreateClient(), email);

        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }
}
