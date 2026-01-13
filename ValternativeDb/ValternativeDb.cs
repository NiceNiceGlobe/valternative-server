using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ValternativeServer.Models.Auth;
using ValternativeServer.Models.Recruiters;

namespace ValternativeServer.ValternativeDb
{
    public class ValternativeDbContext
        : IdentityDbContext<ValternativeUser, Role, Guid>
    {
        public ValternativeDbContext(DbContextOptions<ValternativeDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ValternativeUser>().ToTable("Users", "auth");
            builder.Entity<Role>().ToTable("Roles", "auth");
            builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles", "auth");
            builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims", "auth");
            builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins", "auth");
            builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims", "auth");
            builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens", "auth");

            builder.Entity<ValternativeUser>()
                .Property(u => u.DateOfCreation)
                .ValueGeneratedOnAdd();

            builder.Entity<Recruiter>(entity =>
            {
                entity.ToTable("Recruiters", "Recruiters");

                entity.HasKey(r => r.Id);

                entity.Property(r => r.RecruiterCode)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(r => r.Status)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(r => r.JoinedAt)
                    .IsRequired();

                entity.HasOne(r => r.User)
                    .WithMany()
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<RecruiterSubmission>(entity =>
            {
                entity.ToTable("Submissions", "Recruiters");

                entity.HasKey(s => s.Id);

                entity.Property(s => s.FileName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(s => s.TotalRiders)
                    .IsRequired();

                entity.Property(s => s.Status)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Pending");

                entity.Property(s => s.UploadedAt)
                    .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(s => s.Recruiter)
                    .WithMany(r => r.Submissions)
                    .HasForeignKey(s => s.RecruiterId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<RecruiterRider>(entity =>
            {
                entity.ToTable("Riders", "Recruiters");

                entity.HasKey(r => r.Id);

                entity.Property(r => r.FullName)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(r => r.Email)
                    .HasMaxLength(150);

                entity.Property(r => r.PhoneNumber)
                    .HasMaxLength(50);

                entity.Property(r => r.City)
                    .HasMaxLength(100);

                entity.Property(r => r.Nationality)
                    .HasMaxLength(100);

                entity.Property(r => r.Status)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Pending");

                entity.Property(r => r.CreatedAt)
                    .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(r => r.Submission)
                    .WithMany(s => s.Riders)
                    .HasForeignKey(r => r.SubmissionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<RecruiterBooking>(entity =>
            {
                entity.ToTable("Bookings", "Recruiters");

                entity.HasKey(b => b.Id);

                entity.Property(b => b.RiderId)
                    .IsRequired();

                entity.Property(b => b.RiderName)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(b => b.BookingDate)
                    .IsRequired();

                entity.Property(b => b.BookingTime)
                    .IsRequired();

                entity.Property(b => b.BookingType)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(b => b.Status)
                    .IsRequired()
                    .HasMaxLength(30)
                    .HasDefaultValue("Pending");

                entity.Property(b => b.CreatedAt)
                    .HasDefaultValueSql("SYSUTCDATETIME()");
            });

            builder.Entity<DeployedRider>(entity =>
            {
                entity.ToTable("DeployedRiders", "Recruiters");

                entity.HasKey(d => d.Id);

                entity.Property(d => d.City)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(d => d.Recruiter)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(d => d.BikeRegistration)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(d => d.Agent)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(d => d.Status)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasDefaultValue("Deployed");

                entity.Property(d => d.DeploymentDate)
                    .IsRequired();

                entity.Property(d => d.CreatedAt)
                    .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(d => d.Rider)
                    .WithMany()
                    .HasForeignKey(d => d.RiderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<RecruiterNotificationSettings>(entity =>
            {
                entity.ToTable("NotificationSettings", "Recruiters");

                entity.HasKey(n => n.Id);

                entity.Property(n => n.SubmissionStatusUpdates)
                    .HasDefaultValue(true);

                entity.Property(n => n.WeeklyPerformanceReport)
                    .HasDefaultValue(true);

                entity.Property(n => n.SystemUpdates)
                    .HasDefaultValue(true);

                entity.HasOne(n => n.Recruiter)
                    .WithOne()
                    .HasForeignKey<RecruiterNotificationSettings>(n => n.RecruiterId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        public DbSet<Recruiter> Recruiters { get; set; }
        public DbSet<RecruiterSubmission> RecruiterSubmissions { get; set; }
        public DbSet<RecruiterRider> RecruiterRiders { get; set; }
        public DbSet<RecruiterBooking> RecruiterBookings { get; set; }
        public DbSet<DeployedRider> DeployedRiders { get; set; }
        public DbSet<RecruiterNotificationSettings> RecruiterNotificationSettings { get; set; }
    }
}