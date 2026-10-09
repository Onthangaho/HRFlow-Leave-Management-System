using HRFlow.Application.Services;
using HRFlow.Domain.Interfaces.Services;
using System.Text;
using System.Security.Claims;
using FluentValidation;
using HRFlow.Application.Behaviors;
using HRFlow.Application.Features.Employees.Commands.CreateEmployee;
using HRFlow.Application.Interfaces;
using HRFlow.Domain.Interfaces.Auth;
using HRFlow.Application.Validators.Auth;
using HRFlow.Infrastructure.Extensions;
using HRFlow.Infrastructure.Seeding;
using HRFlow.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using HRFlow.Domain.Interfaces;
using HRFlow.Api.Filters;
using HRFlow.Api.Services;
using HRFlow.Api.Middleware;
using HRFlow.Domain.Entities;
using HRFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

// Allow a small, explicit tolerance for server clock drift rather than the library's five-minute default.
const int JwtClockSkewSeconds = 30;
const string HrAdministratorRoleName = "HR Administrator";
const string HrAdministratorOnlyPolicyName = "HrAdministratorOnly";

var builder = WebApplication.CreateBuilder(args);
DeploymentConfiguration.Validate(builder);
DeploymentStorage.Validate(builder.Configuration, builder.Environment);
using var maintenanceLease = HRFlow.Infrastructure.Services.Operations.MaintenanceLease.ForApplication(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMediatR(configuration =>
    configuration.RegisterServicesFromAssemblyContaining<CreateEmployeeCommand>());
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

builder.Services.AddScoped<CurrentAccountAuthorization>();
builder.Services.AddScoped<ReferenceDataService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IRequestCorrelationContext, RequestCorrelationContext>();
builder.Services.AddScoped<ICurrentEmployeeProvider, CurrentEmployeeProvider>();
builder.Services.AddScoped<IInitiatingCredential, InitiatingCredential>();
builder.Services.AddScoped<HRFlow.Application.Features.LeaveConfiguration.LeaveConfigurationService>();


builder.Services.AddControllers(options => options.Filters.Add<HttpGlobalExceptionFilter>());

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    var signingKey = builder.Configuration["Authentication:Jwt:SigningKey"];
    if (string.IsNullOrEmpty(signingKey))
    {
        throw new InvalidOperationException("JWT SigningKey is not configured.");
    }
    o.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();
            await Results.Problem(statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication required",
                detail: "Sign in with an active account to access this resource.",
                instance: context.Request.Path).ExecuteAsync(context.HttpContext);
        },
        OnForbidden = context => Results.Problem(statusCode: StatusCodes.Status403Forbidden,
            title: "Forbidden", detail: "Your current account permissions do not allow this operation.",
            instance: context.Request.Path).ExecuteAsync(context.HttpContext),
        OnTokenValidated = async context =>
        {
            var access = context.HttpContext.RequestServices.GetRequiredService<IAccountAccessService>();
            if (!await access.IsCurrentAsync(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), context.Principal?.FindFirstValue(CredentialClaims.Version), context.HttpContext.RequestAborted))
            {
                context.Fail("This account is inactive or unavailable.");
                return;
            }
            // Role claims are only an early gate; handlers repeat live checks inside database protection.
            var roles = context.HttpContext.RequestServices.GetRequiredService<IEmployeeRoleLookupService>();
            var principal = context.Principal!;
            var currentRoles = await roles.GetRolesByIdentityUserIdAsync(
                principal.FindFirstValue(ClaimTypes.NameIdentifier)!, context.HttpContext.RequestAborted);
            foreach (var identity in principal.Identities)
                foreach (var claim in identity.FindAll(identity.RoleClaimType).ToArray()) identity.RemoveClaim(claim);
            var primaryIdentity = (ClaimsIdentity)principal.Identity!;
            primaryIdentity.AddClaims(currentRoles.Select(role => new Claim(primaryIdentity.RoleClaimType, role)));
        }
    };
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["Authentication:Jwt:Issuer"],
        ValidAudience = builder.Configuration["Authentication:Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        RequireExpirationTime = true,
        RequireSignedTokens = true,
        ClockSkew = TimeSpan.FromSeconds(JwtClockSkewSeconds),
        ValidateIssuerSigningKey = true,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
    };
});

builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build())
    .AddPolicy(
        HrAdministratorOnlyPolicyName,
        policy => policy.RequireRole(HrAdministratorRoleName));

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("document-upload", context => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, token) => new ValueTask(Results.Problem(statusCode: StatusCodes.Status429TooManyRequests,
        title: "Too many attempts", detail: "Wait a minute before trying again.").ExecuteAsync(context.HttpContext));
    options.AddPolicy("password-change", context => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
        { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("activation", context => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
        { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
    app.Use(async (context, next) =>
    {
        if (!context.Request.IsHttps && !context.Request.Path.StartsWithSegments("/health"))
        {
            await Results.Problem(statusCode: 400, title: "HTTPS required", detail: "Use the configured secure application origin.").ExecuteAsync(context);
            return;
        }
        await next();
    });
}

app.UseCors("ConfiguredOrigins");

app.UseMiddleware<ApiRequestLoggingMiddleware>();
// HR management denials from JWT authorization also need form-ready ProblemDetails, before MVC runs.
app.UseWhen(context => context.Request.Path.StartsWithSegments("/api/v1/management"), branch =>
    branch.UseStatusCodePages(async statusContext =>
    {
        var response = statusContext.HttpContext.Response;
        if (response.StatusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
        {
            await Results.Problem(statusCode: response.StatusCode,
                title: response.StatusCode == StatusCodes.Status403Forbidden ? "Forbidden" : "Authentication required",
                detail: response.StatusCode == StatusCodes.Status403Forbidden
                    ? "HR Administrator access is required to manage leave types and policies."
                    : "Sign in before accessing leave management.",
                instance: statusContext.HttpContext.Request.Path).ExecuteAsync(statusContext.HttpContext);
        }
    }));
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .AllowAnonymous();

app.MapGet("/health/ready", async (HRFlowDbContext db, CancellationToken token) =>
{
    try
    {
        return await db.Database.CanConnectAsync(token) && !(await db.Database.GetPendingMigrationsAsync(token)).Any()
            ? Results.Ok(new { status = "ready" }) : Results.StatusCode(503);
    }
    catch { return Results.StatusCode(503); }
}).AllowAnonymous();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.SeedAsync();
}

app.Run();
