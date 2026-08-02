using HRFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRFlow.Infrastructure.Configurations;

/// <summary>
/// Configures server-side refresh token persistence used for secure token rotation and invalidation.
/// </summary>
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(refreshToken => refreshToken.Id);

        builder.Property(refreshToken => refreshToken.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(refreshToken => refreshToken.Token)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(refreshToken => refreshToken.CreatedAtUtc)
            .IsRequired();

        builder.Property(refreshToken => refreshToken.ExpiresAtUtc)
            .IsRequired();

        builder.HasIndex(refreshToken => refreshToken.Token)
            .IsUnique();

        builder.HasIndex(refreshToken => refreshToken.UserId);

        builder.ToTable("RefreshTokens");
    }
}