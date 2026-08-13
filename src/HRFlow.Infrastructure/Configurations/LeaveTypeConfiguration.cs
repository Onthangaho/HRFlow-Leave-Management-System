using HRFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRFlow.Infrastructure.Configurations;

public class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
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