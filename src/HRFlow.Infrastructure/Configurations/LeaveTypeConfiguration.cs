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
            .IsRequired();

        builder.HasOne(lt => lt.LeavePolicy)
            .WithMany(lp => lp.LeaveTypes)
            .HasForeignKey(lt => lt.LeavePolicyId)
            .IsRequired();
    }
}