using HRFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRFlow.Infrastructure.Configurations;

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.HasKey(lr => lr.Id);

        builder.Property(lr => lr.StartDate)
            .IsRequired();

        builder.Property(lr => lr.EndDate)
            .IsRequired();

        builder.Property(lr => lr.Status)
            .IsRequired();

        builder.HasOne(lr => lr.Employee)
            .WithMany()
            .HasForeignKey(lr => lr.EmployeeId);

        builder.HasOne(lr => lr.LeaveType)
            .WithMany()
            .HasForeignKey(lr => lr.LeaveTypeId);
    }
}