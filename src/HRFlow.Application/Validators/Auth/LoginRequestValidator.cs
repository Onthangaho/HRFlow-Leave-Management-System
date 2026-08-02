using FluentValidation;
using HRFlow.Application.DTOs.Auth;

namespace HRFlow.Application.Validators.Auth;

/// <summary>
/// Enforces basic request shape rules before credential validation reaches identity storage.
/// </summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    /// <summary>
    /// Configures input rules for login payloads to reject malformed requests early.
    /// </summary>
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(request => request.Password)
            .NotEmpty();
    }
}