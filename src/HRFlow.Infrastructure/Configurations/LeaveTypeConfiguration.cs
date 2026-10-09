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
        builder.Property(lt => lt.DescriptionMode).HasDefaultValue(RequirementModes.NotRequested);
        builder.Property(lt => lt.EvidenceMode).HasDefaultValue(RequirementModes.Optional);
        builder.Property(lt => lt.EvidenceClass).HasDefaultValue(SupportingDocumentClass.Ordinary);
        builder.Property(lt => lt.RequirementInstructions).HasMaxLength(RequirementModes.MaxTextLength);
        builder.ToTable(table => table.HasCheckConstraint("CK_LeaveTypes_Requirements", "DescriptionMode IN ('NotRequested','Optional','Required') AND EvidenceMode IN ('NotRequested','Optional','Required') AND EvidenceClass IN ('Medical','Ordinary') AND NOT (EvidenceClass = 'Medical' AND EvidenceMode = 'Required') AND (RequirementInstructions IS NULL OR length(RequirementInstructions) <= 1000)"));

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
