using FluentValidation;
using HRFlow.Application.DTOs.Auth;
using HRFlow.Application.Interfaces.Auth;
using HRFlow.Application.Models.Auth;
using HRFlow.Application.Validators.Auth;
using HRFlow.Infrastructure.Extensions;
using HRFlow.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
builder.Services.AddScoped<IValidator<RefreshTokenRequest>, RefreshTokenRequestValidator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost(
    "/api/v1/auth/login",
    async (
        LoginRequest request,
        IValidator<LoginRequest> validator,
        IAuthService authService,
        CancellationToken cancellationToken) =>
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var authResult = await authService.LoginAsync(request, cancellationToken);
        if (!authResult.Succeeded)
        {
            return Results.Problem(
                title: "Invalid credentials",
                detail: "Email or password is incorrect.",
                statusCode: StatusCodes.Status401Unauthorized,
                type: "https://www.rfc-editor.org/rfc/rfc7807");
        }

        return Results.Ok(authResult.TokenResponse);
    });

app.MapPost(
    "/api/v1/auth/refresh",
    async (
        RefreshTokenRequest request,
        IValidator<RefreshTokenRequest> validator,
        IAuthService authService,
        CancellationToken cancellationToken) =>
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var authResult = await authService.RefreshAsync(request, cancellationToken);
        if (!authResult.Succeeded)
        {
            return authResult.FailureReason switch
            {
                AuthFailureReason.InvalidRefreshToken => Results.Problem(
                    title: "Invalid refresh token",
                    detail: "Refresh token is invalid, expired, or already rotated.",
                    statusCode: StatusCodes.Status401Unauthorized,
                    type: "https://www.rfc-editor.org/rfc/rfc7807"),
                _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
            };
        }

        return Results.Ok(authResult.TokenResponse);
    });

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<HRFlow.Infrastructure.Persistence.HRFlowDbContext>();
    await dbContext.Database.MigrateAsync();
}

await app.Services.SeedDevelopmentAdministratorAsync();

app.Run();
