using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Data;

/// <summary>Creates/migrates the database and seeds demo data (demo password: Password123!).</summary>
public static class DbInitializer
{
    public static void Initialise(RaceDayDbContext db)
    {
        if (db.Database.GetMigrations().Any()) db.Database.Migrate();
        else db.Database.EnsureCreated();

        if (db.Users.Any()) return;

        var pw = PasswordService.Hash("Password123!");
        var org = new User
        {
            FullName = "Thabo Mokoena", Email = "thabo.mokoena@raceday.co.za", PasswordHash = pw, Role = Roles.Organiser,
            Organiser = new Organiser { OrganisationName = "Midrand Runners Club", ContactPhone = "011 555 0101" }
        };
        var org2 = new User
        {
            FullName = "Lindiwe Dube", Email = "lindiwe.dube@raceday.co.za", PasswordHash = pw, Role = Roles.Organiser,
            Organiser = new Organiser { OrganisationName = "Braamfontein Athletics", ContactPhone = "011 555 0202" }
        };
        var p1 = new User
        {
            FullName = "Sipho Nkosi", Email = "sipho.nkosi@example.com", PasswordHash = pw, Role = Roles.Participant,
            Participant = new Participant { DateOfBirth = new DateOnly(1998, 4, 12), EmergencyContact = "Nomsa Nkosi - 082 555 0303" }
        };
        var p2 = new User
        {
            FullName = "Amanda van Wyk", Email = "amanda.vanwyk@example.com", PasswordHash = pw, Role = Roles.Participant,
            Participant = new Participant { DateOfBirth = new DateOnly(2001, 9, 27), EmergencyContact = "Pieter van Wyk - 083 555 0404" }
        };
        db.Users.AddRange(org, org2, p1, p2);

        var year = DateTime.Today.Year + 1;
        org.Organiser!.Events.Add(new Event
        {
            Name = "Midrand Spring Run", EventDate = new DateOnly(year, 10, 10), Location = "Midrand, Gauteng",
            Description = "Annual spring road race through Midrand.", DistanceKm = 10, EventType = "run",
            Status = EventStatuses.Published,
            Categories =
            {
                new Category { Name = "5km Fun Run", DistanceKm = 5, MaxParticipants = 200, EntryFee = 100 },
                new Category { Name = "10km Race", DistanceKm = 10, MaxParticipants = 150, EntryFee = 150 }
            }
        });
        org.Organiser.Events.Add(new Event
        {
            Name = "Sandton Night Cycle", EventDate = new DateOnly(year, 11, 14), Location = "Sandton, Gauteng",
            Description = "Evening cycle with a glow-in-the-dark theme.", DistanceKm = 30, EventType = "cycle",
            Status = EventStatuses.Published,
            Categories = { new Category { Name = "30km Ride", DistanceKm = 30, MaxParticipants = 300, EntryFee = 120 } }
        });
        org2.Organiser!.Events.Add(new Event
        {
            Name = "Braamfontein City Walk", EventDate = new DateOnly(year, 12, 5), Location = "Braamfontein, Gauteng",
            Description = "Urban heritage walk.", DistanceKm = 8, EventType = "walk", Status = EventStatuses.Draft,
            Categories = { new Category { Name = "8km Walk", DistanceKm = 8, MaxParticipants = 100, EntryFee = 80 } }
        });
        db.SaveChanges();
    }
}
