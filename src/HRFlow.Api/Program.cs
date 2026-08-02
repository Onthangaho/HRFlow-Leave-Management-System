using System.Text;
using FluentValidation;
using HRFlow.Application.DTOs.Auth;
using HRFlow.Application.Interfaces.Auth;
using HRFlow.Application.Models.Auth;
using HRFlow.Application.Validators.Auth;
using HRFlow.Infrastructure.Extensions;
using HRFlow.Infrastructure.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "ReactDevServer",
        policy => policy
            .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
builder.Services.AddScoped<IValidator<RefreshTokenRequest>, RefreshTokenRequestValidator>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var signingKey = builder.Configuration["Authentication:Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException(
                "Authentication:Jwt:SigningKey must be configured via appsettings or user secrets.");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Authentication:Jwt:Issuer"],
            ValidAudience = builder.Configuration["Authentication:Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseCors("ReactDevServer");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .AllowAnonymous();

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
    })
    .AllowAnonymous();

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
    })
    .AllowAnonymous();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<HRFlow.Infrastructure.Persistence.HRFlowDbContext>();
    await dbContext.Database.MigrateAsync();
}

await app.Services.SeedDevelopmentAdministratorAsync();

app.Run();
