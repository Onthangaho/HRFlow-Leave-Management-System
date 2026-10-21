using HRFlow.Application.Features.Employees.Queries.GetEmployees;
using HRFlow.Domain.Common;
using HRFlow.Domain.Entities;
using HRFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Infrastructure.Services.Employees;

/// <summary>Bounds materialization before batched Identity roles; all queries use the caller's deferred snapshot.</summary>
public sealed class EmployeeDirectoryReader(HRFlowDbContext db, TimeProvider clock) : IEmployeeDirectoryReader
{
    private const int MaximumPageSize = 100;
    private static void ValidatePage(int page, int size)
    {
        if (page < 1 || size is < 1 or > MaximumPageSize || (long)(page - 1) * size > int.MaxValue)
            throw new DomainException("Page must be positive and pageSize between 1 and 100, with a supported offset.");
    }
    private static string Search(string? value)
    {
        var text = value?.Trim() ?? "";
        if (text.Length > 100) throw new DomainException("Search must contain at most 100 characters.");
        return new string(text.Select(c => c is >= 'A' and <= 'Z' ? (char)(c + 32) : c).ToArray());
    }
    private sealed class Row
    {
        public Employee Employee { get; init; } = null!;
        public DirectoryAccount? User { get; init; }
        public bool Facts { get; init; }
        public bool Schedule { get; init; }
        public bool FutureOnly { get; init; }
    }
    private sealed class DirectoryAccount
    {
        public Guid Id { get; set; }
        public string IdentityId { get; set; } = "";
        public bool RequiresActivation { get; set; }
        public string InvitationDeliveryState { get; set; } = "";
        public DateTime? ActivationExpiresAtUtc { get; set; }
        public DateTime? ActivatedAtUtc { get; set; }
    }
    // SQLite stores GUID keys as TEXT. Reading that key as text avoids CAST/function joins on the indexed Identity side.
    // Only allowlisted account metadata is selected; credentials never enter the directory projection.
    private IQueryable<DirectoryAccount> Accounts() => db.Database.SqlQueryRaw<DirectoryAccount>(
        "SELECT Id, Id AS IdentityId, RequiresActivation, InvitationDeliveryState, ActivationExpiresAtUtc, ActivatedAtUtc FROM AspNetUsers");
    private IQueryable<Row> Rows(DateTimeOffset asOf)
    {
        var today = HRFlow.Application.Features.LeaveRequests.Queries.GetPersonalDashboard.GetPersonalDashboardQueryHandler.BusinessDate(asOf);
        return from e in db.Employees.AsNoTracking()
               join u in Accounts() on EF.Functions.Collate(e.IdentityUserId!, "NOCASE") equals u.IdentityId into accounts
               from u in accounts.DefaultIfEmpty()
               let missing = e.IsActive && e.EmploymentStartDate != null && e.EmploymentStartDate <= today
                   && !db.WeeklyScheduleRevisions.Any(s => s.EmployeeId == e.Id && s.EffectiveFrom <= today)
               select new Row { Employee = e, User = u,
                   Facts = e.IsActive && (e.EmployeeNumber == null || e.EmploymentStartDate == null), Schedule = missing,
                   FutureOnly = missing && db.WeeklyScheduleRevisions.Any(s => s.EmployeeId == e.Id && s.EffectiveFrom > today) };
    }
    private async Task<List<GetEmployeeDto>> ProjectAsync(IQueryable<Row> query, DateTimeOffset asOf, CancellationToken token)
    {
        var now = asOf.UtcDateTime;
        var items = await query.Select(x => new GetEmployeeDto {
            Id = x.Employee.Id, FullName = x.Employee.FullName, Email = x.Employee.Email,
            EmployeeNumber = x.Employee.EmployeeNumber, EmploymentStartDate = x.Employee.EmploymentStartDate,
            DepartmentId = x.Employee.DepartmentId, DepartmentName = x.Employee.Department.Name,
            ManagerId = x.Employee.ManagerId, ManagerName = x.Employee.Manager == null ? null : x.Employee.Manager.FullName,
            Version = x.Employee.Version, IsActive = x.Employee.IsActive, IdentityUserId = x.Employee.IdentityUserId,
            NeedsEmploymentFacts = x.Facts, NeedsCurrentSchedule = x.Schedule, HasFutureScheduleOnly = x.FutureOnly,
            RequiresActivation = x.User!.IdentityId != null && x.User.RequiresActivation,
            InvitationDeliveryState = x.User!.IdentityId == null ? "Unavailable" : !x.Employee.IsActive && x.User.RequiresActivation ? "Unavailable" : x.User.RequiresActivation && x.User.ActivationExpiresAtUtc <= now ? "Expired" : x.User.InvitationDeliveryState,
            ActivatedAtUtc = x.User!.IdentityId == null ? null : x.User.ActivatedAtUtc,
            DeactivatedAtUtc = x.Employee.DeactivatedAtUtc, DeactivationReason = x.Employee.DeactivationReason,
            DeactivatedByName = db.Employees.Where(e => e.Id == x.Employee.DeactivatedById).Select(e => e.FullName).FirstOrDefault(),
            // Empty Current rows retain the owner's concurrency generation, but do not represent downloadable content.
            ProfileImageVersion = db.ProfileImages.Where(i => i.OwnerId == x.Employee.Id && i.Status == ProfileImageStatus.Current && i.Size > 0).Select(i => (Guid?)i.Version).FirstOrDefault()
        }).ToListAsync(token);
        var ids = items.Select(x => Guid.TryParse(x.IdentityUserId, out var id) ? id : Guid.Empty).Distinct().ToArray();
        if (items.Count > 0)
        {
            var roles = await (from membership in db.UserRoles.AsNoTracking() join role in db.Roles.AsNoTracking() on membership.RoleId equals role.Id
                               where ids.Contains(membership.UserId) && role.Name != null orderby role.Name select new { membership.UserId, role.Name }).ToListAsync(token);
            var lookup = roles.ToLookup(r => r.UserId, r => r.Name!);
            foreach (var item in items)
            {
                item.Roles = Guid.TryParse(item.IdentityUserId, out var id) ? lookup[id].ToArray() : [];
                item.ActivatedAtUtc = item.ActivatedAtUtc is {} activated ? DateTime.SpecifyKind(activated, DateTimeKind.Utc) : null;
                item.DeactivatedAtUtc = item.DeactivatedAtUtc is {} ended ? DateTime.SpecifyKind(ended, DateTimeKind.Utc) : null;
            }
        }
        return items;
    }
    /// <inheritdoc />
    public async Task<EmployeeDirectoryPage> ReadAsync(EmployeeDirectoryFilter filter, CancellationToken token)
    {
        ValidatePage(filter.Page, filter.PageSize);
        var asOf = clock.GetUtcNow(); var text = Search(filter.Search); var query = Rows(asOf); var now = asOf.UtcDateTime;
        if (filter.Status is not ("All" or "Active" or "Inactive")) throw new DomainException("Status must be All, Active or Inactive.");
        if (filter.Status != "All") query = query.Where(x => x.Employee.IsActive == (filter.Status == "Active"));
        if (filter.DepartmentId is {} department)
        {
            if (!await db.Departments.AnyAsync(d => d.Id == department, token)) throw new DomainException("Select an existing department.");
            query = query.Where(x => x.Employee.DepartmentId == department);
        }
        if (filter.ReportsTo is {} manager) query = query.Where(x => x.Employee.ManagerId == manager);
        if (text.Length > 0) query = query.Where(x => x.Employee.FullName.ToLower().Contains(text) || x.Employee.Email.ToLower().Contains(text)
            || x.Employee.EmployeeNumber != null && x.Employee.EmployeeNumber.ToLower().Contains(text));
        query = filter.Attention switch {
            "all" => query, "employment" => query.Where(x => x.Facts), "schedule" => query.Where(x => x.Schedule),
            "future-schedule" => query.Where(x => x.FutureOnly),
            "activation" => query.Where(x => x.Employee.IsActive && x.User!.IdentityId != null && x.User.RequiresActivation),
            "delivery-failed" => query.Where(x => x.Employee.IsActive && x.User!.IdentityId != null && x.User.RequiresActivation
                && !(x.User.ActivationExpiresAtUtc <= now) && (x.User.InvitationDeliveryState == "DeliveryFailed" || x.User.InvitationDeliveryState == "DeliveryUnknown")),
            "delivery-pending" => query.Where(x => x.Employee.IsActive && x.User!.IdentityId != null && x.User.RequiresActivation
                && !(x.User.ActivationExpiresAtUtc <= now) && x.User.InvitationDeliveryState == "PendingDelivery"),
            _ => throw new DomainException("Select a supported attention filter.") };
        var total = await query.CountAsync(token);
        var sorted = filter.Sort switch {
            "name" => query.OrderBy(x => x.Employee.FullName.ToLower()).ThenBy(x => x.Employee.Id),
            "email" => query.OrderBy(x => x.Employee.Email.ToLower()).ThenBy(x => x.Employee.Id),
            "number" => query.OrderBy(x => x.Employee.EmployeeNumber == null).ThenBy(x => x.Employee.EmployeeNumber).ThenBy(x => x.Employee.Id),
            _ => throw new DomainException("Sort must be name, email or number (ascending).") };
        return new(await ProjectAsync(sorted.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize), asOf, token), total, filter.Page, filter.PageSize, asOf);
    }
    /// <inheritdoc />
    public async Task<GetEmployeeDto?> DetailAsync(Guid id, CancellationToken token)
    {
        var asOf = clock.GetUtcNow(); return (await ProjectAsync(Rows(asOf).Where(x => x.Employee.Id == id), asOf, token)).SingleOrDefault();
    }
    /// <inheritdoc />
    public async Task<ManagerChoices> ManagersAsync(Guid departmentId, Guid? employeeId, string? search, int page, CancellationToken token)
    {
        const int size = 25; ValidatePage(page, size); var text = Search(search);
        if (!await db.Departments.AnyAsync(d => d.Id == departmentId, token)) throw new DomainException("Select an existing department.");
        var query = Rows(clock.GetUtcNow()).Where(x => x.Employee.DepartmentId == departmentId && x.Employee.IsActive && x.User!.IdentityId != null && !x.User.RequiresActivation
            && x.Employee.Id != employeeId && db.UserRoles.Any(m => m.UserId == x.User.Id && db.Roles.Any(r => r.Id == m.RoleId && r.Name == "Manager")));
        if (text.Length > 0) query = query.Where(x => x.Employee.FullName.ToLower().Contains(text) || x.Employee.Email.ToLower().Contains(text)
            || x.Employee.EmployeeNumber != null && x.Employee.EmployeeNumber.ToLower().Contains(text));
        var total = await query.CountAsync(token);
        var choices = await query.OrderBy(x => x.Employee.FullName.ToLower()).ThenBy(x => x.Employee.Id).Skip((page - 1) * size).Take(size)
            .Select(x => new ManagerChoice(x.Employee.Id, x.Employee.FullName)).ToArrayAsync(token);
        var assigned = employeeId.HasValue ? await db.Employees.Where(e => e.Id == employeeId).Select(e => e.Manager)
            .Where(e => e != null).Select(e => new ManagerChoice(e!.Id, e.FullName)).SingleOrDefaultAsync(token) : null;
        return new(choices, total, page, size, assigned);
    }
}
