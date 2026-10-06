using HRFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRFlow.Infrastructure.Configurations;

/// <summary>Persists shared rules with application-managed edit versions and nonnegative entitlement.</summary>
public class LeavePolicyConfiguration : IEntityTypeConfiguration<LeavePolicy>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LeavePolicy> builder)
    {
        builder.HasKey(lp => lp.Id);
        builder.Property(lp => lp.Name).IsRequired().HasMaxLength(LeavePolicy.MaxNameLength);
        builder.Property(lp => lp.Version).IsConcurrencyToken();
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_LeavePolicies_Balance", "DefaultBalance >= 0");
            table.HasCheckConstraint("CK_LeavePolicies_Name", "length(trim(Name)) BETWEEN 1 AND 100");
        });
    }
}
