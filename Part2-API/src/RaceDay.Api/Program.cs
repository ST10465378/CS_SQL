using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<RaceDayDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("RaceDayDb")));

// Server-side session management: the session stores the user's ID and role after login.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.Cookie.Name = ".RaceDay.Session";
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
    o.IdleTimeout = TimeSpan.FromMinutes(60);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "RESTful API for the RaceDay race-event system. Log in via POST /api/auth/login first - " +
                      "the session cookie is then sent automatically by Swagger UI. " +
                      "Demo accounts (password Password123!): thabo.mokoena@raceday.co.za (Organiser), sipho.nkosi@example.com (Participant)."
    });
    o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
    o.MapType<TimeSpan>(() => new OpenApiSchema { Type = "string", Example = new OpenApiString("00:28:45") });
});

var app = builder.Build();

if (app.Configuration.GetValue("Database:AutoInitialise", true))
{
    using var scope = app.Services.CreateScope();
    DbInitializer.Initialise(scope.ServiceProvider.GetRequiredService<RaceDayDbContext>());
}

app.UseSwagger();

app.UseStaticFiles();   // serves /swagger/custom-form.js

app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");

    // Serve our custom index.html from wwwroot instead of Swashbuckle's default
    o.IndexStream = () =>
        System.IO.File.OpenRead(
            System.IO.Path.Combine(app.Environment.WebRootPath, "swagger", "index.html"));
});

app.UseHttpsRedirection();
app.UseSession();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapControllers();

app.Run();

public partial class Program { }
