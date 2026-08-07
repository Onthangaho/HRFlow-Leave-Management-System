using System.Text;
using FluentValidation;
using HRFlow.Application.Behaviors;
using HRFlow.Application.DTOs.Auth;
using HRFlow.Application.Exceptions;
using HRFlow.Application.Features.Employees.Commands.CreateEmployee;
using HRFlow.Application.Features.Employees.Commands.UpdateEmployee;
using HRFlow.Application.Interfaces.Auth;
using HRFlow.Application.Models.Auth;
using HRFlow.Application.Validators.Auth;
using HRFlow.Infrastructure.Extensions;
using HRFlow.Infrastructure.Seeding;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

const string HrAdministratorRoleName = "HR Administrator";
const string HrAdministratorOnlyPolicyName = "HrAdministratorOnly";

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
builder.Services.AddMediatR(configuration =>
    configuration.RegisterServicesFromAssemblyContaining<CreateEmployeeCommand>());
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

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

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build())
    .AddPolicy(
        HrAdministratorOnlyPolicyName,
        policy => policy.RequireRole(HrAdministratorRoleName));

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

app.MapGet(
    "/api/v1/hr/authorization-check",
    () => Results.Ok(new { status = "authorized" }))
    .RequireAuthorization(HrAdministratorOnlyPolicyName);

app.MapPost(
    "/api/v1/employees",
    async (
        CreateEmployeeCommand command,
        ISender sender,
        CancellationToken cancellationToken) =>
    {
        try
        {
            var result = await sender.Send(command, cancellationToken);
            return Results.Created($"/api/v1/employees/{result.EmployeeId}", result);
        }
        catch (ValidationException validationException)
        {
            return Results.ValidationProblem(validationException.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    grouping => grouping.Key,
                    grouping => grouping.Select(error => error.ErrorMessage).ToArray()));
        }
        catch (EmployeeNotFoundException exception)
        {
            return Results.Problem(
                title: "Employee not found",
                detail: exception.Message,
                statusCode: StatusCodes.Status404NotFound,
                type: "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.4");
        }
        catch (DuplicateEmailException exception)
        {
            return Results.Problem(
                title: "Email already in use",
                detail: exception.Message,
                statusCode: StatusCodes.Status409Conflict,
                type: "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.8");
        }
        catch (InvalidOperationException exception)
        {
            return Results.Problem(
                title: "Employee creation failed",
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest,
                type: "https://www.rfc-editor.org/rfc/rfc7807");
        }
    })
    .RequireAuthorization(HrAdministratorOnlyPolicyName);

app.MapPut(
    "/api/v1/employees/{id:guid}",
    async (
        Guid id,
        UpdateEmployeeCommand command,
        ISender sender,
        CancellationToken cancellationToken) =>
    {
        command.EmployeeId = id;

        try
        {
            var result = await sender.Send(command, cancellationToken);
            return Results.Ok(result);
        }
        catch (ValidationException validationException)
        {
            return Results.ValidationProblem(validationException.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    grouping => grouping.Key,
                    grouping => grouping.Select(error => error.ErrorMessage).ToArray()));
        }
        catch (EmployeeNotFoundException exception)
        {
            return Results.Problem(
                title: "Employee not found",
                detail: exception.Message,
                statusCode: StatusCodes.Status404NotFound,
                type: "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.4");
        }
        catch (DuplicateEmailException exception)
        {
            return Results.Problem(
                title: "Email already in use",
                detail: exception.Message,
                statusCode: StatusCodes.Status409Conflict,
                type: "https://www.rfc-editor.org/rfc/rfc7231#section-6.5.8");
        }
        catch (InvalidOperationException exception)
        {
            return Results.Problem(
                title: "Employee update failed",
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest,
                type: "https://www.rfc-editor.org/rfc/rfc7807");
        }
    })
    .RequireAuthorization(HrAdministratorOnlyPolicyName);

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<HRFlow.Infrastructure.Persistence.HRFlowDbContext>();
    await dbContext.Database.MigrateAsync();
    await app.Services.SeedDepartmentsAsync();
}

await app.Services.SeedDevelopmentAdministratorAsync();

app.Run();