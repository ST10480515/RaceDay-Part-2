


using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Models;

namespace RaceDayAPI.Data
{
    public class RaceDayDbContext : DbContext
    {
        public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
        {
        }
        
        public DbSet<Users> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<EventEnrolment> EventEnrolments { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Result> Results { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---------- Users ----------
            modelBuilder.Entity<Users>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("GETDATE()");
                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_Users_Role", "[Role] IN ('Organiser', 'Participant')"));
            });

            // ---------- Events ----------
            modelBuilder.Entity<Event>(entity =>
            {
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                // Restrict avoids SQL Server "multiple cascade paths" errors
                entity.HasOne(e => e.Organiser)
                      .WithMany(u => u.OrganisedEvents)
                      .HasForeignKey(e => e.OrganiserID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Events_Status",
                        "[Status] IN ('Upcoming', 'Ongoing', 'Completed', 'Cancelled')");
                    t.HasCheckConstraint("CK_Events_EventType",
                        "[EventType] IN ('Run', 'Walk', 'Cycle')");
                });
            });

            // ---------- Categories ----------
            modelBuilder.Entity<Category>(entity =>
            {
                entity.Property(c => c.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            modelBuilder.Entity<Event>()
                .HasMany(e => e.Categories)
                .WithOne(c => c.Event)
                .HasForeignKey(c => c.EventID)
                .OnDelete(DeleteBehavior.Cascade);

            // ---------- EventEnrolments ----------
            modelBuilder.Entity<EventEnrolment>(entity =>
            {
                entity.Property(e => e.EnrolmentDate).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

                // A participant can only enrol once per event
                entity.HasIndex(e => new { e.UserID, e.EventID }).IsUnique();

                entity.HasOne(e => e.Event)
                      .WithMany(ev => ev.Enrolments)
                      .HasForeignKey(e => e.EventID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                      .WithMany(u => u.Enrolments)
                      .HasForeignKey(e => e.UserID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Category)
                      .WithMany(c => c.Enrolments)
                      .HasForeignKey(e => e.CategoryID)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Enrolments_Status",
                        "[Status] IN ('Pending', 'Confirmed', 'Withdrawn', 'Disqualified')");
                    t.HasCheckConstraint("CK_Enrolments_PaymentStatus",
                        "[PaymentStatus] IN ('Unpaid', 'Paid', 'Refunded')");
                });
            });

            // ---------- Payments ----------
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.Property(p => p.PaymentDate).HasDefaultValueSql("GETDATE()");
                entity.Property(p => p.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(p => p.Enrolment)
                      .WithMany(e => e.Payments)
                      .HasForeignKey(p => p.EnrolmentID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.ToTable(t => t.HasCheckConstraint("CK_Payments_Status",
                    "[PaymentStatus] IN ('Pending', 'Completed', 'Failed', 'Refunded')"));
            });

            // ---------- Results ----------
            modelBuilder.Entity<Result>(entity =>
            {
                entity.Property(r => r.CreatedAt).HasDefaultValueSql("GETDATE()");

                entity.HasOne(r => r.Enrolment)
                      .WithMany(e => e.Results)
                      .HasForeignKey(r => r.EnrolmentID)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.ToTable(t => t.HasCheckConstraint("CK_Results_Status",
                    "[Status] IN ('Pending', 'Completed', 'DNS', 'Disqualified')"));
            });
        }
    }
}