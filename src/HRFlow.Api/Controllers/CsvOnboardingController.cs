using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace HRFlow.Api.Controllers;
/// <summary>Operator-facing HR transport only; parsing, review and row persistence belong to the onboarding service.</summary>
[ApiController,Authorize(Policy="HrAdministratorOnly"),Route("api/v1/onboarding")]
public sealed class CsvOnboardingController(ICsvOnboardingService service):ControllerBase
{
 private Guid Actor => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:throw new ForbiddenException("Authenticated account required.");
 /// <summary>Returns a password-free UTF-8 template through a current-authorized operation.</summary>
 [HttpGet("template")]
 public async Task<IActionResult> Template(CancellationToken ct)
 {
  await service.TemplatesAsync(Actor,ct);
  using var buffer = new MemoryStream();
  using (var archive = new ZipArchive(buffer,ZipArchiveMode.Create,true))
  {
   using (var writer = new StreamWriter(archive.CreateEntry("hrflow-onboarding-template.csv").Open(),new UTF8Encoding(false)))
    await writer.WriteAsync(OnboardingTemplate.Csv);
   using (var writer = new StreamWriter(archive.CreateEntry("README.txt").Open(),new UTF8Encoding(false)))
    await writer.WriteAsync(OnboardingTemplate.Guidance);
  }
  return File(buffer.ToArray(),"application/zip","hrflow-onboarding-template.zip");
 }
 /// <summary>Exposes headers only after whole-file structural validation.</summary>
 [HttpPost("inspect"),RequestSizeLimit(1114112),RequestFormLimits(MemoryBufferThreshold=1114112,MultipartBodyLengthLimit=1114112)]
 public async Task<IActionResult> Inspect(IFormFile file,CancellationToken ct)=>Ok(await service.InspectAsync(Actor,await ReadAsync(file,ct),ct));
 /// <summary>Persists normalized actor-bound preview; the client never supplies validated rows.</summary>
 [HttpPost("previews"),RequestSizeLimit(1114112),RequestFormLimits(MemoryBufferThreshold=1114112,MultipartBodyLengthLimit=1114112)]
 public async Task<IActionResult> Preview(IFormFile file,[FromForm]string mapping,CancellationToken ct)
 {
  Dictionary<string,string>? fields;try{fields=JsonSerializer.Deserialize<Dictionary<string,string>>(mapping);}catch(JsonException){throw new HRFlow.Domain.Common.DomainException("Mapping must be valid JSON.");}
  var result=await service.PreviewAsync(Actor,await ReadAsync(file,ct),fields!,ct);return CreatedAtAction(nameof(Get),new{id=result.Id},result);
 }
 /// <summary>Reads actor-owned review/outcomes without exposing other HR imports.</summary>
 [HttpGet("previews/{id:guid}")]public async Task<IActionResult> Get(Guid id,CancellationToken ct)=>Ok(await service.GetAsync(Actor,id,ct));
 /// <summary>Explicitly refreshes validation without recreating successful rows.</summary>
 [HttpPost("previews/{id:guid}/revalidate")]public async Task<IActionResult> Revalidate(Guid id,ReviewVersion input,CancellationToken ct)=>Ok(await service.RevalidateAsync(Actor,id,input.ExpectedVersion,ct));
 /// <summary>Commits selected server-validated rows using durable actor-scoped idempotency.</summary>
 [HttpPost("previews/{id:guid}/confirm")]public async Task<IActionResult> Confirm(Guid id,ImportConfirmation input,CancellationToken ct)=>Ok(await service.ConfirmAsync(Actor,id,input.ExpectedVersion,input.IdempotencyKey,input.Rows,ct));
 /// <summary>Stops further commits; existing successful rows are retained.</summary>
 [HttpPost("previews/{id:guid}/cancel")]public async Task<IActionResult> Cancel(Guid id,CancellationToken ct)=>Ok(await service.CancelAsync(Actor,id,ct));
 /// <summary>Downloads minimal row outcome identifiers, never invitations or uploaded personal fields.</summary>
 [HttpGet("previews/{id:guid}/results")]public async Task<IActionResult> Results(Guid id,CancellationToken ct){Response.Headers.CacheControl="no-store";return File(await service.ResultsAsync(Actor,id,ct),"text/csv","onboarding-results.csv");}
 /// <summary>Lists existing unambiguous codes for mapping and HR maintenance.</summary>
 [HttpGet("schedule-templates")]public async Task<IActionResult> Templates(CancellationToken ct)=>Ok(await service.TemplatesAsync(Actor,ct));
 /// <summary>Replaces a loaded template generation without touching imported employee history.</summary>
 [HttpPost("schedule-templates")]public async Task<IActionResult> SaveTemplate(ImportTemplateInput input,CancellationToken ct)=>Ok(await service.SaveTemplateAsync(Actor,input,ct));
 private static async Task<byte[]> ReadAsync(IFormFile file,CancellationToken ct)
 {
  if(file is null||file.Length is 0 or > 1048576)throw new HRFlow.Domain.Common.DomainException("CSV must be nonempty and no larger than 1 MiB.");
  using var buffer=new MemoryStream();await file.CopyToAsync(buffer,ct);return buffer.ToArray();
 }
}
/// <summary>Preserves the review version loaded by an explicit user action.</summary>
public sealed record ReviewVersion(Guid ExpectedVersion);
/// <summary>Selected row numbers refer to server state, never client-submitted employee payloads.</summary>
public sealed record ImportConfirmation(Guid ExpectedVersion,string IdempotencyKey,int[] Rows);
