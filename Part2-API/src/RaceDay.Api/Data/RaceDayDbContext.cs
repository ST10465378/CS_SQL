using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Organiser> Organisers => Set<Organiser>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Table / column names follow the Part 1 ERD and SQL script.
        b.Entity<User>(e =>
        {
            e.ToTable("Users", t => t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser','Participant')"));
            e.HasKey(x => x.UserId);
            e.Property(x => x.UserId).HasColumnName("UserID");
            e.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            e.Property(x => x.Email).HasMaxLength(255).IsRequired();
            e.HasIndex(x => x.Email).IsUnique().HasDatabaseName("UQ_Users_Email");
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(x => x.Role).HasMaxLength(20).IsRequired();
        });

        b.Entity<Organiser>(e =>
        {
            e.ToTable("Organisers");
            e.HasKey(x => x.OrganiserId);
            e.Property(x => x.OrganiserId).HasColumnName("OrganiserID");
            e.Property(x => x.UserId).HasColumnName("UserID");
            e.Property(x => x.OrganisationName).HasMaxLength(150).IsRequired();
            e.Property(x => x.ContactPhone).HasMaxLength(30);
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasOne(x => x.User).WithOne(u => u.Organiser).HasForeignKey<Organiser>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Participant>(e =>
        {
            e.ToTable("Participants");
            e.HasKey(x => x.ParticipantId);
            e.Property(x => x.ParticipantId).HasColumnName("ParticipantID");
            e.Property(x => x.UserId).HasColumnName("UserID");
            e.Property(x => x.EmergencyContact).HasMaxLength(100);
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasOne(x => x.User).WithOne(u => u.Participant).HasForeignKey<Participant>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Event>(e =>
        {
            e.ToTable("Events", t =>
            {
                t.HasCheckConstraint("CK_Events_Status", "[Status] IN ('draft','published','cancelled')");
                t.HasCheckConstraint("CK_Events_EventType", "[EventType] IN ('run','walk','cycle')");
            });
            e.HasKey(x => x.EventId);
            e.Property(x => x.EventId).HasColumnName("EventID");
            e.Property(x => x.OrganiserId).HasColumnName("OrganiserID");
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Location).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.DistanceKm).HasColumnName("DistanceKM").HasColumnType("decimal(6,2)");
            e.Property(x => x.EventType).HasMaxLength(20).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.HasOne(x => x.Organiser).WithMany(o => o.Events).HasForeignKey(x => x.OrganiserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(x => x.CategoryId);
            e.Property(x => x.CategoryId).HasColumnName("CategoryID");
            e.Property(x => x.EventId).HasColumnName("EventID");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.DistanceKm).HasColumnName("DistanceKM").HasColumnType("decimal(5,2)");
            e.Property(x => x.EntryFee).HasColumnType("decimal(8,2)");
            e.HasOne(x => x.Event).WithMany(ev => ev.Categories).HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Enrolment>(e =>
        {
            e.ToTable("Enrolments", t => t.HasCheckConstraint("CK_Enrolments_PaymentStatus",
                "[PaymentStatus] IN ('pending','paid','refunded')"));
            e.HasKey(x => x.EnrolmentId);
            e.Property(x => x.EnrolmentId).HasColumnName("EnrolmentID");
            e.Property(x => x.ParticipantId).HasColumnName("ParticipantID");
            e.Property(x => x.CategoryId).HasColumnName("CategoryID");
            e.Property(x => x.PaymentStatus).HasMaxLength(20).IsRequired();
            e.HasIndex(x => new { x.ParticipantId, x.CategoryId }).IsUnique()
                .HasDatabaseName("UQ_Enrolments_Participant_Category");
            e.HasOne(x => x.Participant).WithMany(p => p.Enrolments).HasForeignKey(x => x.ParticipantId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Category).WithMany(c => c.Enrolments).HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        b.Entity<Result>(e =>
        {
            e.ToTable("Results");
            e.HasKey(x => x.ResultId);
            e.Property(x => x.ResultId).HasColumnName("ResultID");
            e.Property(x => x.EnrolmentId).HasColumnName("EnrolmentID");
            e.Property(x => x.FinishTime).HasColumnType("time(0)");
            e.HasIndex(x => x.EnrolmentId).IsUnique();
            e.HasOne(x => x.Enrolment).WithOne(en => en.Result).HasForeignKey<Result>(x => x.EnrolmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
