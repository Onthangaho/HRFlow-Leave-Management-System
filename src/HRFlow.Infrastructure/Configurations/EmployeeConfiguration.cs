using HRFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRFlow.Infrastructure.Configurations;

/// <summary>
/// Configures the Employee entity for EF Core without introducing EF attributes into the domain model.
/// </summary>
public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(employee => employee.Id);

        builder.Property(employee => employee.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(employee => employee.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasOne(employee => employee.Department)
            .WithMany(department => department.Employees)
            .HasForeignKey(employee => employee.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(employee => employee.Manager)
            .WithMany(employee => employee.DirectReports)
            .HasForeignKey(employee => employee.ManagerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
