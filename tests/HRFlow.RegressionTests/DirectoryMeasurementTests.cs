using System.Diagnostics;
using System.Net;
using System.Text.Json.Nodes;
using Xunit;

namespace HRFlow.RegressionTests;

/// <summary>Local HTTP measurements use the same generated dataset before/after, not a production load claim.</summary>
public sealed class DirectoryMeasurementTests
{
    /// <summary>Separates the first directory call in a fresh process from five serial warm calls.</summary>
    [Theory]
    [InlineData(25)]
    [InlineData(1000)]
    public async Task MeasureDirectory(int additional)
    {
        await using var fixture = await ApiFixture.StartAsync();
        await DirectoryFixtureData.SeedAsync(fixture, additional);
        var token = await fixture.LoginAsync("hr");
        var elapsed = new List<double>(); var bytes = 0; var rows = 0;
        for (var i = 0; i < 6; i++)
        {
            var clock = Stopwatch.StartNew();
            using var response = await fixture.SendAsync(token, "employees?pageSize=25");
            var body = await response.Content.ReadAsByteArrayAsync(); clock.Stop();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode); bytes = body.Length;
            var json = JsonNode.Parse(body)!;
            rows = json is JsonArray array ? array.Count : json["items"]!.AsArray().Count;
            elapsed.Add(Math.Round(clock.Elapsed.TotalMilliseconds, 2));
        }
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { directoryRows = additional + 7, returnedRows = rows,
            responseBytes = bytes, firstDirectoryCallMs = elapsed[0], warmCallsMs = elapsed.Skip(1).ToArray() }));
    }
}
