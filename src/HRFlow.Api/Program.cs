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

const string HrAdministratorRoleName = "HR Administrator";
const string HrAdministratorOnlyPolicyName = "HrAdministratorOnly";

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentEmployeeProvider, CurrentEmployeeProvider>();
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
        OnTokenValidated = async context =>
        {
            var access = context.HttpContext.RequestServices.GetRequiredService<IAccountAccessService>();
            if (!await access.IsActiveAsync(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), context.HttpContext.RequestAborted))
                context.Fail("This account is inactive or unavailable.");
        }
    };
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["Authentication:Jwt:Issuer"],
        ValidAudience = builder.Configuration["Authentication:Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = true
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

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

if (app.Environment.IsDevelopment())
{
    app.UseCors("ReactDevServer");
}

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
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .AllowAnonymous();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.SeedAsync();
}

app.Run();
