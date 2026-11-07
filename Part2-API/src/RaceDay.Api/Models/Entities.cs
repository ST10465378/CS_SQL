namespace RaceDay.Api.Models;

public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

public static class EventStatuses
{
    public const string Draft = "draft";
    public const string Published = "published";
    public const string Cancelled = "cancelled";
    public static readonly string[] All = { Draft, Published, Cancelled };
}

public static class EventTypes
{
    public static readonly string[] All = { "run", "walk", "cycle" };
}

public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = Roles.Participant;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Organiser? Organiser { get; set; }
    public Participant? Participant { get; set; }
}

public class Organiser
{
    public int OrganiserId { get; set; }
    public int UserId { get; set; }
    public string OrganisationName { get; set; } = "";
    public string? ContactPhone { get; set; }
    public User User { get; set; } = null!;
    public List<Event> Events { get; set; } = new();
}

public class Participant
{
    public int ParticipantId { get; set; }
    public int UserId { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public string? EmergencyContact { get; set; }
    public User User { get; set; } = null!;
    public List<Enrolment> Enrolments { get; set; } = new();
}

public class Event
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string Name { get; set; } = "";
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = "";
    public string? Description { get; set; }
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = "run";
    public string Status { get; set; } = EventStatuses.Draft;
    public Organiser Organiser { get; set; } = null!;
    public List<Category> Categories { get; set; } = new();
}

public class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; }
    public decimal EntryFee { get; set; }
    public Event Event { get; set; } = null!;
    public List<Enrolment> Enrolments { get; set; } = new();
}

public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public int CategoryId { get; set; }
    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;
    public string PaymentStatus { get; set; } = "pending";
    public int? BibNumber { get; set; }
    public Participant Participant { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public Result? Result { get; set; }
}

public class Result
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int? Position { get; set; }
    public Enrolment Enrolment { get; set; } = null!;
}
