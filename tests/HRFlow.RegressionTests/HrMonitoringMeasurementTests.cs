using System.Diagnostics;
using System.Net;
using System.Text.Json.Nodes;
using HRFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using HRFlow.Application.Features.LeaveRequests.Queries.GetPendingLeaveRequests;
using HRFlow.Application.Services;
using HRFlow.Application.Interfaces;
using HRFlow.Domain.Interfaces;
using HRFlow.Infrastructure.Persistence;
using HRFlow.Infrastructure.Services;

namespace HRFlow.RegressionTests;

/// <summary>Serial local HTTP samples exclude fixture construction and never print rows or private configuration.</summary>
public sealed class HrMonitoringMeasurementTests
{
    /// <summary>Identical small/large persisted histories compare first endpoint use and five warm calls; not a load benchmark.</summary>
    [Theory]
    [InlineData(20)]
    [InlineData(2000)]
    public async Task Measure(int size)
    {
        await using var fixture = await ApiFixture.StartAsync();
        await using (var db = fixture.Context())
        {
            var type = await db.LeaveTypes.Select(t => t.Id).SingleAsync();
            db.LeaveRequests.AddRange(Enumerable.Range(0, size).Select(i => LeaveRequest.Create(
                fixture.Employee("employee").Id, type, new DateTime(2028, 1, 1).AddDays(i % 400), new DateTime(2028, 1, 1).AddDays(i % 400))));
            await db.SaveChangesAsync();
        }
        var token = await fixture.LoginAsync("hr");
        var timings = new List<double>(); var bytes = 0; var rows = 0;
        for (var i = 0; i < 6; i++)
        {
            var timer = Stopwatch.StartNew();
            using var response = await fixture.SendAsync(token, "leave-requests/monitoring/pending");
            var body = await response.Content.ReadAsByteArrayAsync(); timer.Stop();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = JsonNode.Parse(body)!;
            rows = json["items"]!.AsArray().Count;
            Assert.Equal(size, json["totalCount"]!.GetValue<int>());
            Assert.Equal(Math.Min(size, 25), rows);
            bytes = body.Length; timings.Add(Math.Round(timer.Elapsed.TotalMilliseconds, 2));
        }
        var counter = new Counter();
        await using var measured = new HRFlowDbContext(new DbContextOptionsBuilder<HRFlowDbContext>().UseSqlite("Data Source=" + fixture.DatabasePath).AddInterceptors(counter).Options);
        var access = new AccountAccessService(measured); var user = fixture.User("hr");
        var reads = new SqliteTeamLeaveReadTransaction(measured, new RequestCredentialValidator(new Credential(user.Id.ToString(), user.CredentialVersion.ToString()), access));
        var handler = new GetPendingLeaveRequestsQueryHandler(measured, reads, new CurrentAccountAuthorization(measured, new EmployeeRoleLookupService(measured), access), TimeProvider.System);
        await handler.Handle(new() { ActorIdentityId = user.Id }, default);
        Assert.Equal(6, counter.Count);
        Assert.True(counter.OneTransaction);
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { monitoringRows = size, returnedRows = rows,
            responseBytes = bytes, firstCallMs = timings[0], warmCallsMs = timings.Skip(1).ToArray(), protectedSelects = counter.Count, counter.OneTransaction }));
    }

    private sealed record Credential(string? IdentityUserId, string? Version) : IInitiatingCredential;
    private sealed class Counter : DbCommandInterceptor
    {
        public int Count; public bool OneTransaction = true; private DbTransaction? transaction;
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData data, InterceptionResult<DbDataReader> result, CancellationToken token = default)
        { Count++; transaction ??= command.Transaction; OneTransaction &= command.Transaction != null && ReferenceEquals(transaction, command.Transaction); return ValueTask.FromResult(result); }
    }
}
