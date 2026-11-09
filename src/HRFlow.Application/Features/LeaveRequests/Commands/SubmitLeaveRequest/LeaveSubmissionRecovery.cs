using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentValidation;
using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces;
using HRFlow.Application.Services;
using HRFlow.Domain.Interfaces;
using HRFlow.Domain.Models.Employees;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveRequests.Commands.SubmitLeaveRequest;

/// <summary>Versioned canonical hash stores no second copy of descriptions. Date ticks retain submitted wall-clock values without timezone conversion.</summary>
public static class LeaveSubmissionIdentity
{
    private const int CanonicalFormatVersion = 1;
    /// <summary>Ordering and surrounding description whitespace are not business changes; reviewed versions and exact date values are.</summary>
    public static string Hash(SubmitLeaveRequestCommand input) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        JsonSerializer.Serialize(new { Format = CanonicalFormatVersion, input.LeaveTypeId, Start = input.StartDate.Ticks, End = input.EndDate.Ticks,
            input.ExpectedTypeVersion, input.ExpectedPolicyVersion, Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            Documents = input.DocumentIds.Distinct().OrderBy(id => id).ToArray() }))));

    /// <summary>Caller must already hold a credential-checked snapshot or writer reservation; every lookup rechecks live personal access and the request association.</summary>
    public static async Task<Guid?> FindAsync(IApplicationDbContext context, CurrentAccountAuthorization authorization,
        SubmitLeaveRequestCommand input, string hash, CancellationToken token)
    {
        await authorization.RequireEmployeeAsync(input.EmployeeId, [EmployeeRoles.Employee, EmployeeRoles.Manager], token);
        if (input.SubmissionKey == null) return null;
        var previous = await context.LeaveSubmissionOutcomes.AsNoTracking()
            .SingleOrDefaultAsync(s => s.OwnerId == input.EmployeeId && s.Key == input.SubmissionKey, token);
        if (previous == null) return null;
        if (previous.PayloadHash != hash) throw new WriteConflictException("This submission key belongs to different request inputs. Recover the original attempt; do not reuse its key for edits.");
        if (!await context.LeaveRequests.AsNoTracking().AnyAsync(r => r.Id == previous.RequestId && r.EmployeeId == input.EmployeeId, token))
            throw new NotFoundException("Submission outcome is unavailable to this account.");
        return previous.RequestId;
    }
}

/// <summary>Explicitly checks an existing attempt only; never executes a missing submission.</summary>
public sealed record CheckLeaveSubmissionOutcomeQuery(SubmitLeaveRequestCommand Attempt) : IRequest<Guid?>;

/// <summary>Outcome inspection shares current credentials, personal permission and association reads in one deferred snapshot.</summary>
public sealed class CheckLeaveSubmissionOutcomeQueryHandler(IApplicationDbContext context, CurrentAccountAuthorization authorization,
    ILeaveReportingReadTransaction reads, IValidator<SubmitLeaveRequestCommand> validator)
    : IRequestHandler<CheckLeaveSubmissionOutcomeQuery, Guid?>
{
    /// <inheritdoc />
    public async Task<Guid?> Handle(CheckLeaveSubmissionOutcomeQuery request, CancellationToken token)
    {
        await validator.ValidateAndThrowAsync(request.Attempt, token);
        if (request.Attempt.SubmissionKey == null) throw new ValidationException("A submission key is required to check an outcome.");
        var hash = LeaveSubmissionIdentity.Hash(request.Attempt);
        return await reads.ExecuteAsync(ct => LeaveSubmissionIdentity.FindAsync(context, authorization, request.Attempt, hash, ct), token);
    }
}
