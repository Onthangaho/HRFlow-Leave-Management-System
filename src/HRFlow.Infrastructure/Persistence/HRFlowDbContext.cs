using HRFlow.Domain.Models.Auth;
using HRFlow.Domain.Interfaces;
using HRFlow.Domain.Entities;
using HRFlow.Infrastructure.Configurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Infrastructure.Persistence;

/// <summary>
/// Persists application data and the ASP.NET Core Identity schema for the HRFlow backend.
/// Provides access to employees linked to identity users and all domain entities.
/// </summary>
public sealed class HRFlowDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext, HRFlow.Application.Interfaces.IApplicationDocumentContext
{
    /// <summary>
    /// Creates a new EF Core context for HRFlow with the supplied options.
    /// </summary>
    public HRFlowDbContext(DbContextOptions<HRFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<SupportingDocument> SupportingDocuments => Set<SupportingDocument>();
    public DbSet<DocumentAccessEntry> DocumentAccessEntries => Set<DocumentAccessEntry>();
    public DbSet<AccountSettings> AccountSettings => Set<AccountSettings>();
    public DbSet<LeaveNotificationEvent> LeaveNotificationEvents => Set<LeaveNotificationEvent>();
    public DbSet<LeaveNotification> LeaveNotifications => Set<LeaveNotification>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
        public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<LeavePolicy> LeavePolicies => Set<LeavePolicy>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    /// <summary>
    /// Gets refresh token records used for server-side token rotation and invalidation.
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<SupportingDocument>().HasIndex(d => new { d.OwnerId, d.UploadKey }).IsUnique();
        modelBuilder.Entity<SupportingDocument>().HasOne<Employee>().WithMany().HasForeignKey(d => d.OwnerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupportingDocument>().HasOne<LeaveRequest>().WithMany().HasForeignKey(d => d.RequestId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SupportingDocument>().Property(d => d.Version).IsConcurrencyToken();
        modelBuilder.Entity<DocumentAccessEntry>().HasOne<SupportingDocument>().WithMany().HasForeignKey(d => d.DocumentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AccountSettings>().HasKey(s => s.EmployeeId);
        modelBuilder.Entity<AccountSettings>().HasOne<Employee>().WithOne().HasForeignKey<AccountSettings>(s => s.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AccountSettings>().Property(s => s.PreferredDisplayName).HasMaxLength(HRFlow.Domain.Entities.AccountSettings.MaxPreferredNameLength);
        modelBuilder.Entity<AccountSettings>().Property(s => s.ContactPhone).HasMaxLength(HRFlow.Domain.Entities.AccountSettings.MaxContactPhoneLength);
        modelBuilder.Entity<AccountSettings>().Property(s => s.Theme).HasMaxLength(10);
        modelBuilder.Entity<LeaveNotificationEvent>().Property(e => e.EventKey).HasMaxLength(100);
        modelBuilder.Entity<LeaveNotificationEvent>().Property(e => e.Kind).HasMaxLength(32);
        modelBuilder.Entity<LeaveNotificationEvent>().HasIndex(e => new { e.EventKey, e.RecipientId }).IsUnique();
        modelBuilder.Entity<LeaveNotificationEvent>().HasIndex(e => new { e.DeliveredAtUtc, e.CreatedAtUtc });
        modelBuilder.Entity<LeaveNotificationEvent>().HasOne<LeaveRequest>().WithMany().HasForeignKey(e => e.RequestId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<LeaveNotification>().HasIndex(n => new { n.EventId, n.RecipientId }).IsUnique();
        modelBuilder.Entity<LeaveNotification>().HasIndex(n => new { n.RecipientId, n.ReadAtUtc, n.CreatedAtUtc });
        modelBuilder.Entity<LeaveNotification>().HasOne(n => n.Event).WithMany().HasForeignKey(n => n.EventId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ApplicationUser>().Property(u => u.RequiresActivation).HasDefaultValue(false);
        modelBuilder.Entity<ApplicationUser>().Property(u => u.InvitationDeliveryState).HasMaxLength(32).HasDefaultValue(ActivationDeliveryStates.NotRequired);
        modelBuilder.Entity<ApplicationUser>().Property(u => u.ActivationTokenHash).HasMaxLength(64);
        modelBuilder.Entity<ApplicationUser>().HasIndex(u => u.ActivationTokenHash).IsUnique();

        modelBuilder.ApplyConfiguration(new EmployeeConfiguration());
        modelBuilder.ApplyConfiguration(new DepartmentConfiguration());
        modelBuilder.ApplyConfiguration(new LeaveTypeConfiguration());
        modelBuilder.ApplyConfiguration(new LeavePolicyConfiguration());
        modelBuilder.ApplyConfiguration(new LeaveRequestConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.Entity<AuditEntry>().Property(a => a.CorrelationId).HasMaxLength(AuditEntry.MaxCorrelationIdLength);
        modelBuilder.Entity<AuditEntry>().Property(a => a.DecisionNote).HasMaxLength(AuditEntry.MaxDecisionNoteLength);
        modelBuilder.Entity<AuditEntry>().Property(a => a.Reason).HasMaxLength(AuditEntry.MaxReasonLength);
        modelBuilder.Entity<AuditEntry>()
            .HasIndex(auditEntry => new
            {
                auditEntry.LeaveRequestId,
                auditEntry.Action,
                auditEntry.ActorId,
                auditEntry.OldStatus,
                auditEntry.NewStatus
            })
            .IsUnique();
    }
}
