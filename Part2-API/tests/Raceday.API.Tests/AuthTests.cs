using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Api.Tests;

public class AuthTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Register_Participant_Returns201_AndNeverReturnsPassword()
    {
        var c = factory.CreateClient();
        var res = await Api.Register(c, "Participant", Api.NewEmail());
        var body = await res.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        Assert.Contains("\"role\":\"Participant\"", body);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_Organiser_Returns201()
    {
        var res = await Api.Register(factory.CreateClient(), "Organiser", Api.NewEmail());
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var c = factory.CreateClient();
        var email = Api.NewEmail();
        await Api.Register(c, "Participant", email);
        var res = await Api.Register(c, "Participant", email);
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { fullName = "X Y", email = Api.NewEmail(), password = Api.Password, role = "Admin" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var res = await Api.Register(factory.CreateClient(), "Participant", Api.NewEmail(), "short");
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_OrganiserWithoutOrganisationName_Returns400()
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { fullName = "Org Person", email = Api.NewEmail(), password = Api.Password, role = "Organiser" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_ParticipantWithoutDateOfBirth_Returns400()
    {
        var res = await factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new { fullName = "Run Ner", email = Api.NewEmail(), password = Api.Password, role = "Participant" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotPlainText()
    {
        var email = Api.NewEmail();
        await Api.Register(factory.CreateClient(), "Participant", email);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var stored = db.Users.Single(u => u.Email == email).PasswordHash;

        Assert.NotEqual(Api.Password, stored);
        Assert.DoesNotContain(Api.Password, stored);
        Assert.StartsWith("v1.", stored);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200_WithUserAndRole()
    {
        var c = factory.CreateClient();
        var email = Api.NewEmail();
        await Api.Register(c, "Organiser", email);

        var res = await Api.Login(c, email);
        var json = await Api.Json(res);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("Organiser", json.GetProperty("user").GetProperty("role").GetString());
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var c = factory.CreateClient();
        var email = Api.NewEmail();
        await Api.Register(c, "Participant", email);
        var res = await Api.Login(c, email, "WrongPassword1!");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var res = await Api.Login(factory.CreateClient(), Api.NewEmail());
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_CreatesSession_SoProtectedEndpointWorks()
    {
        var c = await Api.LoggedInClient(factory, "Participant");
        var res = await c.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsSession()
    {
        var c = await Api.LoggedInClient(factory, "Participant");
        var logout = await c.PostAsync("/api/auth/logout", null);
        var after = await c.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
    }
}
