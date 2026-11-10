using System.Diagnostics;
using System.Net;
using System.Text.Json.Nodes;
using HRFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRFlow.RegressionTests;

/// <summary>Serial local HTTP samples exclude fixture construction and never print rows or private configuration.</summary>
public sealed class PersonalHistoryMeasurementTests
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
        var token = await fixture.LoginAsync("employee");
        var timings = new List<double>(); var bytes = 0; var rows = 0;
        for (var i = 0; i < 6; i++)
        {
            var timer = Stopwatch.StartNew();
            using var response = await fixture.SendAsync(token, "leave-requests/history");
            var body = await response.Content.ReadAsByteArrayAsync(); timer.Stop();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = JsonNode.Parse(body)!;
            rows = json is JsonArray array ? array.Count : json["items"]!.AsArray().Count;
            bytes = body.Length; timings.Add(Math.Round(timer.Elapsed.TotalMilliseconds, 2));
        }
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { historyRows = size, returnedRows = rows,
            responseBytes = bytes, firstCallMs = timings[0], warmCallsMs = timings.Skip(1).ToArray() }));
    }
}
