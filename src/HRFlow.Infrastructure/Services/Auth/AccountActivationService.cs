using HRFlow.Domain.Models.Auth;
using HRFlow.Domain.Common;
using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;
using HRFlow.Application.Interfaces;
using HRFlow.Application.Exceptions;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Exceptions;
using HRFlow.Domain.Models.Employees;
using HRFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
namespace HRFlow.Infrastructure.Services.Auth;
/// <summary>Hash-only, purpose-bound one-time activation integrated with Identity and SQLite protection.</summary>
public sealed class AccountActivationService(HRFlowDbContext context, UserManager<ApplicationUser> users,
    IEmployeeManagementTransaction transaction, ITeamLeaveReadTransaction reads, IActivationDelivery delivery, IConfiguration configuration) : IAccountActivationService
{
    /// <inheritdoc />
    public async Task<ActivationStatus> GetStatusAsync(string? identityId, CancellationToken token)
    {
        var user = Guid.TryParse(identityId, out var id) ? await context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, token) : null;
        return user is null ? new(false, ActivationDeliveryStates.Unavailable, null) : new(user.RequiresActivation, await CurrentStatusAsync(user, token),
            user.ActivatedAtUtc is {} date ? DateTime.SpecifyKind(date, DateTimeKind.Utc) : null);
    }
    private const string InvalidInvitation = "This invitation is invalid, expired, or no longer available. Ask HR for a new invitation.";
    private const int TokenBytes = 32;
    private const int TokenLength = TokenBytes * 2;
    private const int MaximumPasswordLength = 256;
    private const int DefaultExpiryMinutes = 24 * 60;
    private const int MinimumExpiryMinutes = 5;
    private const int MaximumExpiryMinutes = 7 * 24 * 60;
    /// <inheritdoc />
    public void EnsureDeliveryConfigured()
    {
        delivery.EnsureConfigured();
        var origin = configuration["Activation:ApplicationOrigin"];
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.UserInfo.Length != 0 || uri.Query.Length != 0
            || uri.Fragment.Length != 0 || uri.AbsolutePath != "/" || (uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback)))
            throw new DomainException("Configure Activation:ApplicationOrigin as a trusted HTTPS origin (loopback HTTP is allowed for development).");
        _ = GetLifetime();
        _ = DeliveryTimeout();
    }
    private int GetLifetime() => int.TryParse(configuration["Activation:ExpiryMinutes"] ?? DefaultExpiryMinutes.ToString(System.Globalization.CultureInfo.InvariantCulture), out var minutes)
        && minutes is >= MinimumExpiryMinutes and <= MaximumExpiryMinutes ? minutes : throw new DomainException("Activation:ExpiryMinutes must be between 5 and 10080.");
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("HRFlow:FirstPassword:v1:" + value)));
    /// <inheritdoc />
    public string Prepare(ApplicationUser user)
    {
        var value = Convert.ToHexString(RandomNumberGenerator.GetBytes(TokenBytes));
        user.ActivationTokenHash = Hash(value);
        user.ActivationRecipientNormalizedEmail = users.NormalizeEmail(user.Email);
        user.ActivationDispatchStartedAtUtc = null;
        user.ActivationExpiresAtUtc = DateTime.UtcNow.AddMinutes(GetLifetime());
        user.InvitationDeliveryState = ActivationDeliveryStates.PendingDelivery;
        return value;
    }
    /// <inheritdoc />
    public async Task<string> DeliverAsync(Guid actorId, Guid userId, string invitation, CancellationToken token)
    {
        var hash = Hash(invitation);
        string? recipient = null;
        string? normalized = null;
        var status = ActivationDeliveryStates.Unavailable;
        // The hash is the invitation generation. This claim prevents another process dispatching it again.
        await transaction.ExecuteAsync(async ct =>
        {
            await RequireHrAsync(actorId, ct);
            var current = await context.Users.SingleOrDefaultAsync(u => u.Id == userId, ct);
            status = await CurrentStatusAsync(current, ct);
            if (current is null || current.ActivationTokenHash != hash || !await EligibleAsync(current, ct)
                || current.ActivationDispatchStartedAtUtc is not null) return;
            normalized = current.ActivationRecipientNormalizedEmail;
            if (normalized is null || normalized != users.NormalizeEmail(current.Email)) return;
            if (current.Email is null || current.Email.Length > 256 || current.Email.IndexOfAny(['\r', '\n']) >= 0
                || !MailAddress.TryCreate(current.Email, out var address) || address.Address != current.Email)
            { current.InvitationDeliveryState = status = ActivationDeliveryStates.DeliveryFailed; return; }
            recipient = current.Email;
            current.ActivationDispatchStartedAtUtc = DateTime.UtcNow;
        }, token);
        if (recipient is null) return status;
        var outcome = ActivationDeliveryOutcome.Unknown;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(DeliveryTimeout()));
        try
        {
            // A final read narrows the race without holding SQLite protection during network I/O.
            // Mail already dispatched cannot be recalled by an edit that commits immediately afterward.
            var eligible = await reads.ExecuteAsync(async ct =>
            {
                await RequireHrAsync(actorId, ct);
                var current = await context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId, ct);
                return current is not null && current.ActivationTokenHash == hash
                    && current.ActivationRecipientNormalizedEmail == normalized && users.NormalizeEmail(current.Email) == normalized
                    && await EligibleAsync(current, ct);
            }, deadline.Token);
            if (!eligible) return await ReadAuthoritativeAsync(actorId, userId, token);
            var link = new Uri(configuration["Activation:ApplicationOrigin"]!.TrimEnd('/') + "/activate#token=" + invitation);
            outcome = await delivery.DeliverAsync(userId, recipient, link, deadline.Token).WaitAsync(deadline.Token);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or TimeoutException or OperationCanceledException)
        {
            // A disconnect may follow acceptance. Never log private exception payloads or claim definite failure.
        }
        var delivered = outcome switch
        {
            ActivationDeliveryOutcome.PickupReady => ActivationDeliveryStates.PickupReady,
            ActivationDeliveryOutcome.Accepted => ActivationDeliveryStates.Accepted,
            ActivationDeliveryOutcome.Failed => ActivationDeliveryStates.DeliveryFailed,
            _ => ActivationDeliveryStates.DeliveryUnknown
        };
        try
        {
            await transaction.ExecuteAsync(async ct =>
            {
                await RequireHrAsync(actorId, ct);
                var current = await context.Users.SingleOrDefaultAsync(u => u.Id == userId, ct);
                if (current is not null && current.ActivationTokenHash == hash
                    && current.ActivationRecipientNormalizedEmail == normalized && users.NormalizeEmail(current.Email) == normalized
                    && await EligibleAsync(current, ct)) current.InvitationDeliveryState = delivered;
                status = await CurrentStatusAsync(current, ct);
            }, token);
            return status;
        }
        catch (Exception exception) when (exception is WriteConflictException or DbUpdateException)
        {
            // Creation already committed. Read persisted feedback without replaying the failed acknowledgement.
            return await ReadAuthoritativeAsync(actorId, userId, token);
        }
    }
    private int DeliveryTimeout() => int.TryParse(configuration["Activation:DeliveryTimeoutSeconds"] ?? "15", out var seconds)
        && seconds is >= 1 and <= 30 ? seconds : throw new DomainException("Activation:DeliveryTimeoutSeconds must be between 1 and 30.");
    private async Task RequireHrAsync(Guid actorId, CancellationToken token)
    {
        var actor = await context.Users.SingleOrDefaultAsync(u => u.Id == actorId, token);
        if (actor is null || actor.RequiresActivation || !await users.IsInRoleAsync(actor, EmployeeRoles.HrAdministrator)
            || !await context.Employees.AnyAsync(e => e.IdentityUserId == actorId.ToString() && e.IsActive, token))
            throw new ForbiddenException("Current active HR membership is required for invitation delivery.");
    }
    private Task<bool> EligibleAsync(ApplicationUser user, CancellationToken token) =>
        user.RequiresActivation && user.PasswordHash is null && user.ActivationTokenHash is not null
        && user.ActivationExpiresAtUtc > DateTime.UtcNow
        ? context.Employees.AnyAsync(e => e.IdentityUserId == user.Id.ToString() && e.IsActive, token)
        : Task.FromResult(false);
    private async Task<string> CurrentStatusAsync(ApplicationUser? user, CancellationToken token)
    {
        if (user is null) return ActivationDeliveryStates.Unavailable;
        if (!user.RequiresActivation) return user.InvitationDeliveryState;
        if (!await context.Employees.AnyAsync(e => e.IdentityUserId == user.Id.ToString() && e.IsActive, token)) return ActivationDeliveryStates.Unavailable;
        return user.ActivationExpiresAtUtc <= DateTime.UtcNow ? ActivationDeliveryStates.Expired : user.InvitationDeliveryState;
    }
    private Task<string> ReadAuthoritativeAsync(Guid actorId, Guid userId, CancellationToken token) => reads.ExecuteAsync(async ct =>
    {
        await RequireHrAsync(actorId, ct);
        return await CurrentStatusAsync(await context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId, ct), ct);
    }, token);
    /// <inheritdoc />
    public async Task<string> ResendAsync(Guid actorId, Guid employeeId, Guid expectedVersion, CancellationToken token)
    {
        if (expectedVersion == Guid.Empty) throw new DomainException("ExpectedVersion is required.");
        EnsureDeliveryConfigured();
        Guid userId = default;
        string invitation = "";
        await transaction.ExecuteAsync(async ct =>
        {
            var actor = await users.FindByIdAsync(actorId.ToString());
            if (actor is null || actor.RequiresActivation || !await users.IsInRoleAsync(actor, EmployeeRoles.HrAdministrator)
                || !await context.Employees.AnyAsync(e => e.IdentityUserId == actorId.ToString() && e.IsActive, ct))
                throw new ForbiddenException("Current active HR membership is required to resend invitations.");
            var employee = await context.Employees.SingleOrDefaultAsync(e => e.Id == employeeId, ct)
                ?? throw new NotFoundException("Employee was not found.");
            if (!employee.IsActive || employee.Version != expectedVersion)
                throw new WriteConflictException("Employee is inactive or changed. Reload before resending.");
            var account = employee.IdentityUserId is null ? null : await users.FindByIdAsync(employee.IdentityUserId);
            if (account is null || !account.RequiresActivation || account.PasswordHash is not null)
                throw new WriteConflictException("This account is not awaiting activation.");
            userId = account.Id;
            invitation = Prepare(account);
            // Rotate the existing profile version so simultaneous resends with the same version cannot both proceed.
            employee.Update(employee.FullName, employee.Email, employee.DepartmentId);
        }, token);
        return await DeliverAsync(actorId, userId, invitation, token);
    }
    /// <inheritdoc />
    public async Task RedeemAsync(string invitation, string password, CancellationToken token)
    {
        if (invitation is null || invitation.Length != TokenLength || !invitation.All(Uri.IsHexDigit)) throw new DomainException(InvalidInvitation);
        if (string.IsNullOrEmpty(password) || password.Length > MaximumPasswordLength) throw new DomainException("Enter a password of at most 256 characters.");
        var hash = Hash(invitation);
        await transaction.ExecuteAsync(async ct =>
        {
            var user = await context.Users.SingleOrDefaultAsync(u => u.ActivationTokenHash == hash, ct);
            if (user is null || !user.RequiresActivation || user.PasswordHash is not null || user.ActivationExpiresAtUtc <= DateTime.UtcNow
                || user.ActivationExpiresAtUtc is null || !await context.Employees.AnyAsync(e => e.IdentityUserId == user.Id.ToString() && e.IsActive, ct))
                throw new DomainException(InvalidInvitation);
            var result = await users.AddPasswordAsync(user, password);
            if (!result.Succeeded) throw new DomainException(string.Join(" ", result.Errors.Select(e => e.Description)));
            user.RequiresActivation = false;
            user.ActivatedAtUtc = DateTime.UtcNow;
            user.ActivationTokenHash = null;
            user.ActivationRecipientNormalizedEmail = null;
            user.ActivationDispatchStartedAtUtc = null;
            user.ActivationExpiresAtUtc = null;
            user.InvitationDeliveryState = ActivationDeliveryStates.Activated;
        }, token);
    }
}
