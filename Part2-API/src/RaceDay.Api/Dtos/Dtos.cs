using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

/// <summary>Details needed to create an account.</summary>
public class RegisterRequest
{
    [Required, StringLength(150)] public string FullName { get; set; } = "";
    [Required, EmailAddress, StringLength(255)] public string Email { get; set; } = "";
    [Required, MinLength(8), StringLength(100)] public string Password { get; set; } = "";
    /// <summary>Either "Organiser" or "Participant".</summary>
    [Required] public string Role { get; set; } = "";
    /// <summary>Required for Organisers.</summary>
    [StringLength(150)] public string? OrganisationName { get; set; }
    [StringLength(30)] public string? ContactPhone { get; set; }
    /// <summary>Required for Participants (yyyy-MM-dd).</summary>
    public DateOnly? DateOfBirth { get; set; }
    [StringLength(100)] public string? EmergencyContact { get; set; }
    /// <summary>Only needed when the server is configured with an organiser registration code.</summary>
    public string? OrganiserCode { get; set; }
}

public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

public class UpdateProfileRequest
{
    [Required, StringLength(150)] public string FullName { get; set; } = "";
    [StringLength(150)] public string? OrganisationName { get; set; }
    [StringLength(30)] public string? ContactPhone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    [StringLength(100)] public string? EmergencyContact { get; set; }
}

public record UserResponse(int UserId, string FullName, string Email, string Role, string? OrganisationName,
    string? ContactPhone, DateOnly? DateOfBirth, string? EmergencyContact, DateTime CreatedAt);

public record LoginResponse(string Message, UserResponse User);

public class EventRequest
{
    [Required, StringLength(150)] public string Name { get; set; } = "";
    public DateOnly EventDate { get; set; }
    [Required, StringLength(200)] public string Location { get; set; } = "";
    [StringLength(1000)] public string? Description { get; set; }
    /// <summary>Overall event distance in km.</summary>
    [Range(0.01, 1000)] public decimal DistanceKm { get; set; }
    /// <summary>One of: run, walk, cycle.</summary>
    [Required] public string EventType { get; set; } = "";
    /// <summary>draft, published or cancelled (defaults to draft).</summary>
    public string? Status { get; set; }
}

public class EventResponse
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string OrganisationName { get; set; } = "";
    public string Name { get; set; } = "";
    public DateOnly EventDate { get; set; }
    public string Location { get; set; } = "";
    public string? Description { get; set; }
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = "";
    public string Status { get; set; } = "";
    public List<CategoryResponse>? Categories { get; set; }
}

public class CategoryRequest
{
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Range(0.01, 1000)] public decimal DistanceKm { get; set; }
    /// <summary>0 means unlimited.</summary>
    [Range(0, 100000)] public int MaxParticipants { get; set; }
    [Range(0, 100000)] public decimal EntryFee { get; set; }
}

public record CategoryResponse(int CategoryId, int EventId, string Name, decimal DistanceKm, int MaxParticipants,
    decimal EntryFee, int EnrolledCount);

public record EnrolmentResponse(int EnrolmentId, int ParticipantId, string ParticipantName, int CategoryId,
    string CategoryName, int EventId, string EventName, DateOnly EventDate, DateTime EnrolmentDate,
    string PaymentStatus, int? BibNumber);

public class ResultRequest
{
    /// <summary>Finish time as hh:mm:ss, e.g. 00:28:45.</summary>
    public TimeSpan FinishTime { get; set; }
    /// <summary>Finishing position within the category (optional).</summary>
    [Range(1, 1000000)] public int? Position { get; set; }
}

public record ResultResponse(int ResultId, int EnrolmentId, string ParticipantName, int? BibNumber, int CategoryId,
    TimeSpan FinishTime, int? Position);

/// <summary>EF-translatable projections shared by the controllers.</summary>
public static class Projections
{
    public static readonly Expression<Func<Event, EventResponse>> EventDto = e => new EventResponse
    {
        EventId = e.EventId, OrganiserId = e.OrganiserId, OrganisationName = e.Organiser.OrganisationName,
        Name = e.Name, EventDate = e.EventDate, Location = e.Location, Description = e.Description,
        DistanceKm = e.DistanceKm, EventType = e.EventType, Status = e.Status
    };

    public static readonly Expression<Func<Category, CategoryResponse>> CategoryDto = c =>
        new CategoryResponse(c.CategoryId, c.EventId, c.Name, c.DistanceKm, c.MaxParticipants, c.EntryFee,
            c.Enrolments.Count);

    public static readonly Expression<Func<Enrolment, EnrolmentResponse>> EnrolmentDto = e =>
        new EnrolmentResponse(e.EnrolmentId, e.ParticipantId, e.Participant.User.FullName, e.CategoryId,
            e.Category.Name, e.Category.EventId, e.Category.Event.Name, e.Category.Event.EventDate,
            e.EnrolmentDate, e.PaymentStatus, e.BibNumber);

    public static readonly Expression<Func<Result, ResultResponse>> ResultDto = r =>
        new ResultResponse(r.ResultId, r.EnrolmentId, r.Enrolment.Participant.User.FullName,
            r.Enrolment.BibNumber, r.Enrolment.CategoryId, r.FinishTime, r.Position);
}

public static class UserMapper
{
    public static UserResponse ToResponse(this User u) =>
        new(u.UserId, u.FullName, u.Email, u.Role, u.Organiser?.OrganisationName, u.Organiser?.ContactPhone,
            u.Participant?.DateOfBirth, u.Participant?.EmergencyContact, u.CreatedAt);
}
