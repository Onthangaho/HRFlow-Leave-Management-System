using System.Text.Json;
using HRFlow.Application.Interfaces;
using HRFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace HRFlow.Infrastructure.Services.Employees;
/// <summary>Removes expired preview personal fields in bounded cross-process transactions, retaining minimal committed row outcomes.</summary>
public sealed class OnboardingCleanupWorker(IServiceScopeFactory scopes,ILogger<OnboardingCleanupWorker> logger):BackgroundService
{
 private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
 /// <inheritdoc />
 protected override async Task ExecuteAsync(CancellationToken stoppingToken)
 {
  while(!stoppingToken.IsCancellationRequested)
  {
   try
   {
    using var scope=scopes.CreateScope();var db=scope.ServiceProvider.GetRequiredService<HRFlowDbContext>();
    await scope.ServiceProvider.GetRequiredService<IEmployeeManagementTransaction>().ExecuteAsync(async ct=>{
     var cutoff=DateTime.UtcNow.AddDays(-1);
     var batches=await db.OnboardingBatches.Where(b=>b.ExpiresAtUtc<cutoff&&b.MappingJson!="").OrderBy(b=>b.ExpiresAtUtc).Take(20).ToListAsync(ct);
     foreach(var b in batches){var rows=JsonSerializer.Deserialize<List<ImportRow>>(b.RowsJson,Json)!;b.RowsJson=JsonSerializer.Serialize(rows.Select(r=>r with{Fields=new(),Errors=[]}),Json);b.MappingJson="";b.Cancelled=true;b.Version=Guid.NewGuid();}
    },stoppingToken);
   }
   catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested){return;}
   catch(Exception ex){logger.LogWarning("Onboarding cleanup deferred. Category: {Category}",ex.GetType().Name);}
   await Task.Delay(TimeSpan.FromHours(1),stoppingToken);
  }
 }
}
