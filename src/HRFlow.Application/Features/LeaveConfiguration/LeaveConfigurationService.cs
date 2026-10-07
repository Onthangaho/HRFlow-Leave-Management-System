using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces;
using HRFlow.Domain.Common;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Interfaces;
using HRFlow.Domain.Interfaces.Services;
using HRFlow.Domain.Models.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRFlow.Application.Features.LeaveConfiguration;

/// <summary>Validates shared rules and history references inside the same writer protection used by approvals.</summary>
public sealed class LeaveConfigurationService(
    IApplicationDbContext context,
    ILeaveConfigurationTransaction transaction,
    IEmployeeRoleLookupService roles,
    ILogger<LeaveConfigurationService> logger)
{
    /// <summary>Returns management snapshots with current rules and deletion consequences.</summary>
    public async Task<IReadOnlyList<LeaveTypeManagementDto>> GetTypesAsync(Guid? id, CancellationToken token)
    {
        var types = await context.LeaveTypes.AsNoTracking()
            .Where(type => !id.HasValue || type.Id == id)
            .OrderBy(type => type.Name)
            .Select(type => new LeaveTypeManagementDto(type.Id, type.Name, type.Version,
                type.LeavePolicyId, type.LeavePolicy.Name, type.LeavePolicy.Version,
                type.LeavePolicy.AllowOverlap, type.LeavePolicy.DefaultBalance,
                context.LeaveRequests.Count(request => request.LeaveTypeId == type.Id),
                !context.LeaveRequests.Any(request => request.LeaveTypeId == type.Id)))
            .ToListAsync(token);
        if (id.HasValue && types.Count == 0) throw new NotFoundException("Leave type was not found.");
        return types;
    }

    /// <summary>Shows all linked types so HR can understand the reach of a policy edit.</summary>
    public async Task<IReadOnlyList<PolicyManagementDto>> GetPoliciesAsync(Guid? id, CancellationToken token)
    {
        var policies = await context.LeavePolicies.AsNoTracking()
            .Where(policy => !id.HasValue || policy.Id == id)
            .OrderBy(policy => policy.Name)
            .Select(policy => new PolicyManagementDto(policy.Id, policy.Name, policy.Version,
                policy.AllowOverlap, policy.DefaultBalance,
                policy.LeaveTypes.OrderBy(type => type.Name).Select(type => new LinkedLeaveTypeDto(
                    type.Id, type.Name, context.LeaveRequests.Count(request => request.LeaveTypeId == type.Id))).ToList(),
                !policy.LeaveTypes.Any()))
            .ToListAsync(token);
        if (id.HasValue && policies.Count == 0) throw new NotFoundException("Leave policy was not found.");
        return policies;
    }

    /// <summary>Creates or replaces shared policy rules after live HR authorization and stale-edit checks.</summary>
    public async Task<PolicyManagementDto> SavePolicyAsync(Guid actor, Guid? id, Guid? expectedVersion,
        string name, bool allowOverlap, int defaultBalance, CancellationToken token)
    {
        PolicyManagementDto? result = null;
        await transaction.ExecuteAsync(async protectedToken =>
        {
            await EnsureHrAsync(actor, protectedToken);
            LeavePolicy policy;
            if (id.HasValue)
            {
                policy = await FindPolicyAsync(id.Value, protectedToken);
                EnsureVersion(policy.Version, expectedVersion);
                policy.Update(name, allowOverlap, defaultBalance);
            }
            else
            {
                policy = LeavePolicy.Create(name, allowOverlap, defaultBalance);
                context.LeavePolicies.Add(policy);
            }
            await context.SaveChangesAsync(protectedToken);
            result = (await GetPoliciesAsync(policy.Id, protectedToken)).Single();
        }, token);
        logger.LogInformation("Leave policy saved. PolicyId: {PolicyId}; ActorIdentityUserId: {ActorIdentityUserId}", result!.Id, actor);
        return result;
    }

    /// <summary>Resolves an explicit policy and serializes normalized-name uniqueness with other type writes.</summary>
    public async Task<LeaveTypeManagementDto> SaveTypeAsync(Guid actor, Guid? id, Guid? expectedVersion,
        string name, Guid policyId, CancellationToken token)
    {
        LeaveTypeManagementDto? result = null;
        await transaction.ExecuteAsync(async protectedToken =>
        {
            await EnsureHrAsync(actor, protectedToken);
            if (policyId == Guid.Empty) throw new DomainException("A nonempty leavePolicyId is required.");
            var policy = await FindPolicyAsync(policyId, protectedToken);
            LeaveType type;
            if (id.HasValue)
            {
                type = await FindTypeAsync(id.Value, protectedToken);
                EnsureVersion(type.Version, expectedVersion);
                type.Update(name, policy);
            }
            else type = LeaveType.Create(name, policy);
            if (await context.LeaveTypes.AnyAsync(other => other.Id != type.Id
                && other.NormalizedName == type.NormalizedName, protectedToken))
                throw new WriteConflictException("A leave type with this name already exists. Choose another name.");
            if (!id.HasValue) context.LeaveTypes.Add(type);
            await context.SaveChangesAsync(protectedToken);
            result = (await GetTypesAsync(type.Id, protectedToken)).Single();
        }, token);
        logger.LogInformation("Leave type saved. LeaveTypeId: {LeaveTypeId}; ActorIdentityUserId: {ActorIdentityUserId}", result!.Id, actor);
        return result;
    }

    /// <summary>Deletes only unused types; every request status protects its category and audit history.</summary>
    public async Task DeleteTypeAsync(Guid actor, Guid id, Guid expectedVersion, CancellationToken token)
    {
        await transaction.ExecuteAsync(async protectedToken =>
        {
            await EnsureHrAsync(actor, protectedToken);
            var type = await FindTypeAsync(id, protectedToken);
            EnsureVersion(type.Version, expectedVersion);
            if (await context.LeaveRequests.AnyAsync(request => request.LeaveTypeId == id, protectedToken))
                throw new WriteConflictException("This leave type has leave requests, including historical requests, and cannot be deleted.");
            context.LeaveTypes.Remove(type);
        }, token);
        logger.LogInformation("Leave type deleted. LeaveTypeId: {LeaveTypeId}; ActorIdentityUserId: {ActorIdentityUserId}", id, actor);
    }

    /// <summary>Requires HR to resolve all type references explicitly before deleting a policy.</summary>
    public async Task DeletePolicyAsync(Guid actor, Guid id, Guid expectedVersion, CancellationToken token)
    {
        await transaction.ExecuteAsync(async protectedToken =>
        {
            await EnsureHrAsync(actor, protectedToken);
            var policy = await FindPolicyAsync(id, protectedToken);
            EnsureVersion(policy.Version, expectedVersion);
            if (await context.LeaveTypes.AnyAsync(type => type.LeavePolicyId == id, protectedToken))
                throw new WriteConflictException("This policy is linked to leave types and cannot be deleted. Reassign or remove unused types first.");
            context.LeavePolicies.Remove(policy);
        }, token);
        logger.LogInformation("Leave policy deleted. PolicyId: {PolicyId}; ActorIdentityUserId: {ActorIdentityUserId}", id, actor);
    }

    private async Task EnsureHrAsync(Guid actor, CancellationToken token)
    {
        if (!await context.Employees.AnyAsync(e => e.IdentityUserId == actor.ToString() && e.IsActive, token)
            || !(await roles.GetRolesByIdentityUserIdAsync(actor.ToString(), token)).Contains(EmployeeRoles.HrAdministrator))
            throw new ForbiddenException("Current HR Administrator membership is required to manage leave types and policies.");
    }

    private static void EnsureVersion(Guid version, Guid? expectedVersion)
    {
        if (!expectedVersion.HasValue || expectedVersion == Guid.Empty)
            throw new DomainException("A nonempty expectedVersion from the current management record is required.");
        if (version != expectedVersion)
            throw new WriteConflictException("This record changed since you loaded it. Reload before editing or deleting.");
    }

    private async Task<LeavePolicy> FindPolicyAsync(Guid id, CancellationToken token) =>
        await context.LeavePolicies.SingleOrDefaultAsync(policy => policy.Id == id, token)
            ?? throw new NotFoundException("Leave policy was not found.");

    private async Task<LeaveType> FindTypeAsync(Guid id, CancellationToken token) =>
        await context.LeaveTypes.SingleOrDefaultAsync(type => type.Id == id, token)
            ?? throw new NotFoundException("Leave type was not found.");
}
