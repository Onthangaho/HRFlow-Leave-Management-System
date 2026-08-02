using FluentValidation;
using HRFlow.Application.DTOs.Auth;

namespace HRFlow.Application.Validators.Auth;

/// <summary>
/// Ensures refresh requests contain a token value before rotation logic executes.
/// </summary>
public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    /// <summary>
    /// Configures request rules for refresh-token rotation calls.
    /// </summary>
    public RefreshTokenRequestValidator()
    {
        RuleFor(request => request.RefreshToken)
            .NotEmpty();
    }
}