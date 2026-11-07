using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Api.Tests;

/// <summary>Small helpers so each test reads as a clear scenario.</summary>
public static class Api
{
    public const string Password = "Password123!";

    public static string NewEmail(string prefix = "user") => $"{prefix}-{Guid.NewGuid():N}@test.com";
    public static string FutureDate(int days = 30) => DateTime.Today.AddDays(days).ToString("yyyy-MM-dd");

    public static Task<HttpResponseMessage> Register(HttpClient c, string role, string email, string password = Password) =>
        c.PostAsJsonAsync("/api/auth/register", role == "Organiser"
            ? (object)new { fullName = "Test Organiser", email, password, role, organisationName = "Test Club", contactPhone = "011 000 0000" }
            : new { fullName = "Test Participant", email, password, role, dateOfBirth = "1999-05-20", emergencyContact = "Mum - 082 000 0000" });

    public static Task<HttpResponseMessage> Login(HttpClient c, string email, string password = Password) =>
        c.PostAsJsonAsync("/api/auth/login", new { email, password });

    /// <summary>A new client (own cookie jar) that has registered and logged in with the given role.</summary>
    public static async Task<HttpClient> LoggedInClient(ApiFactory f, string role, string? email = null)
    {
        var c = f.CreateClient();
        email ??= NewEmail(role.ToLowerInvariant());
        (await Register(c, role, email)).EnsureSuccessStatusCode();
        (await Login(c, email)).EnsureSuccessStatusCode();
        return c;
    }

    public static async Task<JsonElement> Json(HttpResponseMessage r) => await r.Content.ReadFromJsonAsync<JsonElement>();

    public static object EventBody(string name = "Test Event", string status = "published", string type = "run", string? date = null) =>
        new { name, eventDate = date ?? FutureDate(), location = "Sandton", description = "Test event", distanceKm = 10, eventType = type, status };

    public static async Task<int> CreateEvent(HttpClient organiser, string status = "published")
    {
        var r = await organiser.PostAsJsonAsync("/api/events", EventBody(status: status));
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await Json(r)).GetProperty("eventId").GetInt32();
    }

    public static async Task<int> CreateCategory(HttpClient organiser, int eventId, int max = 100)
    {
        var r = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { name = "10km", distanceKm = 10, maxParticipants = max, entryFee = 100 });
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await Json(r)).GetProperty("categoryId").GetInt32();
    }

    public static async Task<int> Enrol(HttpClient participant, int categoryId)
    {
        var r = await participant.PostAsync($"/api/categories/{categoryId}/enrolments", null);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await Json(r)).GetProperty("enrolmentId").GetInt32();
    }

    /// <summary>A logged-in organiser who owns a published event with one category.</summary>
    public static async Task<(HttpClient Organiser, int EventId, int CategoryId)> OrganiserWithCategory(ApiFactory f, int max = 100)
    {
        var org = await LoggedInClient(f, "Organiser");
        var eventId = await CreateEvent(org);
        var categoryId = await CreateCategory(org, eventId, max);
        return (org, eventId, categoryId);
    }
}
