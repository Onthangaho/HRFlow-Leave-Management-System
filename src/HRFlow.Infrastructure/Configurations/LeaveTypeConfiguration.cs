using HRFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRFlow.Infrastructure.Configurations;

/// <summary>
/// Configures the LeaveType entity for EF Core persistence.
/// </summary>
public class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    /// <summary>
    /// Configures entity properties, keys, and relationships for the LeaveType entity.
    /// </summary>
    public void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        builder.HasKey(lt => lt.Id);

        builder.Property(lt => lt.Name)
            .IsRequired().HasMaxLength(LeaveType.MaxNameLength);
        builder.Property(lt => lt.NormalizedName).IsRequired();
        builder.HasIndex(lt => lt.NormalizedName).IsUnique();
        builder.Property(lt => lt.Version).IsConcurrencyToken();
        builder.ToTable(table => table.HasCheckConstraint("CK_LeaveTypes_Name", "length(trim(Name)) BETWEEN 1 AND 100"));

        builder.HasOne(lt => lt.LeavePolicy)
            .WithMany(lp => lp.LeaveTypes)
            .HasForeignKey(lt => lt.LeavePolicyId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
