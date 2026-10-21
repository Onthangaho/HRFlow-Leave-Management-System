using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces;
using HRFlow.Application.Services;
using HRFlow.Domain.Common;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Exceptions;
using HRFlow.Domain.Models.Employees;
using HRFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Infrastructure.Services.Employees;
/// <summary>Bounded CSV parsing and durable row orchestration reuse the existing creation core and SQLite transaction boundaries.</summary>
public sealed class CsvOnboardingService(HRFlowDbContext db, EmployeeManagementService employees,
 UserManager<ApplicationUser> users, CurrentAccountAuthorization auth, IEmployeeManagementTransaction writes,
 ILeaveReportingReadTransaction reads, IAccountActivationService activation) : ICsvOnboardingService
{
 public const int MaxBytes = 1024 * 1024;
 public const int MaxRows = 100;
 public static readonly string[] Columns = ["FirstName","LastName","EmailAddress","EmployeeNumber","Roles","Department","ManagerEmail","EmploymentStartDate","WorkScheduleCode","Gender"];
 private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
 private static string Encode<T>(T value) => JsonSerializer.Serialize(value,Json);
 private static T Decode<T>(string value) => JsonSerializer.Deserialize<T>(value,Json)!;
 private static ImportPreview View(OnboardingBatch b) => new(b.Id,b.Version,DateTime.SpecifyKind(b.ExpiresAtUtc,DateTimeKind.Utc),b.Cancelled,Decode<List<ImportRow>>(b.RowsJson));
 private async Task<OnboardingBatch> OwnedAsync(Guid actor, Guid id, CancellationToken ct) =>
  await db.OnboardingBatches.SingleOrDefaultAsync(b => b.Id == id && b.ActorId == actor,ct) ?? throw new NotFoundException("Import was not found.");
 private static void Open(OnboardingBatch b) { if(b.Cancelled || b.ExpiresAtUtc <= DateTime.UtcNow) throw new WriteConflictException("This review expired or was cancelled. Upload again for a fresh authorized review."); }
 private static void Version(OnboardingBatch b, Guid version) { if(b.Version != version) throw new WriteConflictException("Review changed. Reload the outcomes and explicitly revalidate before continuing."); }
 private static void Save(OnboardingBatch b, List<ImportRow> rows) { b.RowsJson=Encode(rows);b.Version=Guid.NewGuid(); }
 private static (string[] Headers,List<string[]> Rows) Parse(byte[] bytes)
 {
  if(bytes.Length is 0 or > MaxBytes) throw new DomainException("CSV must be nonempty and no larger than 1 MiB.");
  try
  {
   var text=new UTF8Encoding(false,true).GetString(bytes).TrimStart('\uFEFF');
   if(text.Contains('\0')) throw new DomainException("Only UTF-8 CSV is supported.");
   using var csv=new CsvReader(new StringReader(text),new CsvConfiguration(CultureInfo.InvariantCulture){DetectColumnCountChanges=true,IgnoreBlankLines=false,ExceptionMessagesContainRawData=false,Mode=CsvMode.RFC4180});
   if(!csv.Read()) throw new DomainException("CSV requires a header.");
   csv.ReadHeader();var headers=csv.HeaderRecord!.Select(h=>h.Trim()).ToArray();
   if(headers.Length is 0 or > 30 || headers.Any(h=>h.Length is 0 or > 100) || headers.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=headers.Length)
    throw new DomainException("Headers must be distinct, nonempty and bounded.");
   if(headers.Any(h=>new[]{"password","credential","secret","token","securitystamp","hash","pwd","passphrase","apikey","authorization"}.Any(word=>new string(h.Where(char.IsLetterOrDigit).ToArray()).Contains(word,StringComparison.OrdinalIgnoreCase))))
    throw new DomainException("Password and credential columns are prohibited. Remove them before uploading.");
   var rows=new List<string[]>();while(csv.Read())
   {
    if(rows.Count==MaxRows) throw new DomainException("CSV is limited to 100 data rows.");
    var row=Enumerable.Range(0,headers.Length).Select(i=>csv.GetField(i)??"").ToArray();
    if(row.Any(v=>v.Length>1000 || v.Any(c=>char.IsControl(c)&&c!='\n'&&c!='\r'&&c!='\t'))) throw new DomainException("CSV fields exceed supported limits or contain invalid control characters.");
    rows.Add(row);
   }
   if(rows.Count==0) throw new DomainException("CSV requires at least one data row.");
   return (headers,rows);
  }
  catch(DecoderFallbackException){throw new DomainException("Only valid UTF-8 CSV is supported.");}
  catch(CsvHelperException){throw new DomainException("CSV is malformed. Check quotes and field counts.");}
 }
 /// <inheritdoc />
 public Task<string[]> InspectAsync(Guid actor,byte[] bytes,CancellationToken token) => reads.ExecuteAsync(async ct=>{await auth.RequireIdentityAsync(actor,[EmployeeRoles.HrAdministrator],ct);return Parse(bytes).Headers;},token);
 /// <inheritdoc />
 public async Task<ImportPreview> PreviewAsync(Guid actor,byte[] bytes,Dictionary<string,string> mapping,CancellationToken token)
 {
  var parsed=Parse(bytes);
  if(mapping is null || mapping.Keys.Any(k=>!Columns.Contains(k)) || mapping.Values.Any(v=>!parsed.Headers.Contains(v)) || mapping.Values.Distinct().Count()!=mapping.Count)
   throw new DomainException("Map each source column at most once to a supported field.");
  if(Columns.Take(6).Append("EmploymentStartDate").Any(k=>!mapping.ContainsKey(k))) throw new DomainException("Map all required name, email, number, roles, department and start-date fields.");
  ImportPreview? result=null;
  await writes.ExecuteAsync(async ct=>{
   await auth.RequireIdentityAsync(actor,[EmployeeRoles.HrAdministrator],ct);
   var rows=parsed.Rows.Select((r,i)=>new ImportRow(i+2,mapping.ToDictionary(m=>m.Key,m=>r[Array.IndexOf(parsed.Headers,m.Value)].Trim()),"Invalid",[])).ToList();
   rows=await ValidateRowsAsync(rows,false,ct);
   var batch=new OnboardingBatch{ActorId=actor,SourceHash=Convert.ToHexString(SHA256.HashData(bytes)),MappingJson=Encode(mapping),RowsJson=Encode(rows),ExpiresAtUtc=DateTime.UtcNow.AddMinutes(30)};
   db.OnboardingBatches.Add(batch);result=View(batch);
  },token);return result!;
 }
 private async Task<List<ImportRow>> ValidateRowsAsync(List<ImportRow> rows,bool compareSnapshots,CancellationToken ct)
 {
  var emailCounts=rows.Where(r=>r.State!="Created").GroupBy(r=>users.NormalizeEmail(r.Fields.GetValueOrDefault("EmailAddress","") )).ToDictionary(g=>g.Key!,g=>g.Count());
  var numberCounts=rows.Where(r=>r.State!="Created").GroupBy(r=>r.Fields.GetValueOrDefault("EmployeeNumber","").Trim().ToUpperInvariant()).ToDictionary(g=>g.Key,g=>g.Count());
  var result=new List<ImportRow>();foreach(var row in rows)
  {
   if(row.State=="Created"){result.Add(row);continue;}
   string F(string name)=>row.Fields.GetValueOrDefault(name,"");
   if(F("Gender")!=""){result.Add(row with{State="Invalid",Errors=["Gender storage is not supported. Leave the optional field blank; it is never an eligibility rule."]});continue;}
   if(emailCounts[users.NormalizeEmail(F("EmailAddress"))!]>1 || numberCounts[F("EmployeeNumber").Trim().ToUpperInvariant()]>1)
   {result.Add(row with{State="Duplicate",Errors=["Email or employee number is repeated within this file."]});continue;}
   try
   {
    if(F("Gender")!="") throw new DomainException("Gender storage is not supported in this phase. Remove supplied values; it is never an eligibility rule.");
    if(F("FirstName")==""||F("LastName")=="") throw new DomainException("FirstName and LastName are required and join with one space into FullName.");
    var departmentName=F("Department");
    var dept=await db.Departments.SingleOrDefaultAsync(d=>d.Name==departmentName,ct) ?? throw new DomainException("Department must match an existing name exactly.");
    Employee? manager=null;
    if(F("ManagerEmail")!="")
    {
     var normalized=users.NormalizeEmail(F("ManagerEmail"));var identities=await db.Users.Where(u=>u.NormalizedEmail==normalized||u.NormalizedUserName==normalized).Select(u=>u.Id).ToListAsync(ct);
     if(identities.Count!=1) throw new DomainException("ManagerEmail must identify one existing activated manager.");
     var identity=identities[0].ToString();manager=await db.Employees.SingleOrDefaultAsync(e=>e.IdentityUserId==identity,ct) ?? throw new DomainException("Manager has no employee profile.");
    }
    if(!DateOnly.TryParseExact(F("EmploymentStartDate"),"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var date)) throw new DomainException("EmploymentStartDate must be a real yyyy-MM-dd date.");
    var roleNames=F("Roles").Split(';',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
    var canonicalRoles = await employees.ValidateCreationAsync(F("FirstName")+" "+F("LastName"),F("EmailAddress"),dept.Id,roleNames,manager?.Id,F("EmployeeNumber"),date,ct);
    OnboardingScheduleTemplate? template=null;
    var scheduleCode=F("WorkScheduleCode")==""?null:EmploymentFacts.NormalizeNumber(F("WorkScheduleCode"));
    if(scheduleCode is not null) template=await db.OnboardingScheduleTemplates.SingleOrDefaultAsync(t=>t.Code==scheduleCode,ct) ?? throw new DomainException("WorkScheduleCode must identify an existing template, or be blank for Unknown.");
    if(compareSnapshots&&(row.DepartmentId!=dept.Id||row.ManagerId!=manager?.Id||row.ManagerVersion!=manager?.Version||row.TemplateId!=template?.Id||row.TemplateVersion!=template?.Version))
     throw new DomainException("Department, manager or schedule changed since preview. Revalidate explicitly before retrying.");
    var normalizedFields = new Dictionary<string,string>(row.Fields) { ["EmployeeNumber"] = EmploymentFacts.NormalizeNumber(F("EmployeeNumber")), ["Roles"] = string.Join(";",canonicalRoles), ["WorkScheduleCode"] = scheduleCode ?? "" };
    result.Add(row with{Fields=normalizedFields,State="Ready",Errors=[],DepartmentId=dept.Id,ManagerId=manager?.Id,ManagerVersion=manager?.Version,TemplateId=template?.Id,TemplateVersion=template?.Version});
   }
   catch(DuplicateEmailException){result.Add(row with{State="Duplicate",Errors=["Email is already registered, including inactive or unlinked records."]});}
   catch(WriteConflictException){result.Add(row with{State="Duplicate",Errors=["Email or employee number is already assigned."]});}
   catch(EmployeeManagementValidationException){result.Add(row with{State="Invalid",Errors=["Email/name is not accepted by the configured Identity validators."]});}
   catch(DomainException ex){result.Add(row with{State="Invalid",Errors=[ex.Message]});}
  }return result;
 }
 /// <inheritdoc />
 public async Task<ImportPreview> RevalidateAsync(Guid actor,Guid id,Guid version,CancellationToken token)
 {
  ImportPreview? result=null;await writes.ExecuteAsync(async ct=>{await auth.RequireIdentityAsync(actor,[EmployeeRoles.HrAdministrator],ct);var b=await OwnedAsync(actor,id,ct);Open(b);Version(b,version);Save(b,await ValidateRowsAsync(Decode<List<ImportRow>>(b.RowsJson),false,ct));b.ConfirmationKey=null;result=View(b);},token);return result!;
 }
 /// <inheritdoc />
 public async Task<ImportPreview> ConfirmAsync(Guid actor,Guid id,Guid version,string key,int[] selected,CancellationToken token)
 {
  if(!Guid.TryParse(key,out _)||selected is null||selected.Length is 0 or > MaxRows||selected.Distinct().Count()!=selected.Length) throw new DomainException("Supply a UUID confirmation key and distinct selected Ready rows.");
  activation.EnsureDeliveryConfigured();
  await writes.ExecuteAsync(async ct=>{
   await auth.RequireIdentityAsync(actor,[EmployeeRoles.HrAdministrator],ct);var b=await OwnedAsync(actor,id,ct);Open(b);
   if(b.ConfirmationKey==key){if(!Decode<int[]>(b.SelectedRowsJson).Order().SequenceEqual(selected.Order()))throw new WriteConflictException("This key belongs to a different selection.");return;}
   Version(b,version);if(b.ConfirmationKey is not null)throw new WriteConflictException("Explicit revalidation is required before a new confirmation.");
   var rows=Decode<List<ImportRow>>(b.RowsJson);if(selected.Any(n=>!rows.Any(r=>r.Row==n&&r.State=="Ready")))throw new DomainException("Select only Ready rows from this review.");
   b.ConfirmationKey=key;b.Version=Guid.NewGuid();b.SelectedRowsJson=Encode(selected);
  },token);
  // Durable selection, not replayed client rows, controls work across competing API processes.

  foreach(var number in selected)
  {
   Guid identityId=default;string invitation="";Guid employeeId=default;
   try
   {
    await writes.ExecuteAsync(async ct=>{
     var hr=await auth.RequireIdentityAsync(actor,[EmployeeRoles.HrAdministrator],ct);var b=await OwnedAsync(actor,id,ct);
     if(b.Cancelled||b.ExpiresAtUtc<=DateTime.UtcNow||b.ConfirmationKey!=key)return;
     if(!Decode<int[]>(b.SelectedRowsJson).Contains(number))throw new WriteConflictException("This key belongs to a different selection.");
     var rows=Decode<List<ImportRow>>(b.RowsJson);var index=rows.FindIndex(r=>r.Row==number&&r.State=="Ready");if(index<0)return;
     var validation=(await ValidateRowsAsync(rows,true,ct))[index];
     if(validation.State!="Ready"){rows[index]=validation;Save(b,rows);return;}
     string F(string field)=>validation.Fields.GetValueOrDefault(field,"");
     var created=await employees.CreateInsideReservationAsync(F("FirstName")+" "+F("LastName"),F("EmailAddress"),validation.DepartmentId!.Value,F("Roles").Split(';',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries),validation.ManagerId,F("EmployeeNumber"),DateOnly.ParseExact(F("EmploymentStartDate"),"yyyy-MM-dd",CultureInfo.InvariantCulture),ct);
     employeeId=created.Result.EmployeeId;identityId=created.IdentityId;invitation=created.Invitation;
     if(validation.TemplateId.HasValue)
     {
      var template=await db.OnboardingScheduleTemplates.SingleAsync(t=>t.Id==validation.TemplateId,ct);
      db.WeeklyScheduleRevisions.Add(WeeklyScheduleRevision.Create(employeeId,hr.Id,template.Name,DateOnly.ParseExact(F("EmploymentStartDate"),"yyyy-MM-dd",CultureInfo.InvariantCulture),Decode<int[]>(template.MinutesJson)));
      db.Employees.Local.Single(e=>e.Id==employeeId).ReviseSchedule();
     }
     rows[index]=validation with{State="Created",EmployeeId=employeeId,DeliveryState="Prepared"};Save(b,rows);
    },token);
   }
   catch(DbUpdateException)
   {
    // Unknown persistence failures are not replayed. A separate outcome transaction runs only after rollback.
    await RecordFailureAsync(actor,id,number,key,token);continue;
   }
   if(identityId!=Guid.Empty)
   {
    var state=await activation.DeliverAsync(actor,identityId,invitation,token);
    await writes.ExecuteAsync(async ct=>{await auth.RequireIdentityAsync(actor,[EmployeeRoles.HrAdministrator],ct);var b=await OwnedAsync(actor,id,ct);var rows=Decode<List<ImportRow>>(b.RowsJson);var i=rows.FindIndex(r=>r.Row==number&&r.EmployeeId==employeeId);if(i>=0){rows[i]=rows[i] with{DeliveryState=state};Save(b,rows);}},token);
   }
  }
  return await GetAsync(actor,id,token);
 }
 private Task RecordFailureAsync(Guid actor,Guid id,int row,string key,CancellationToken token)=>writes.ExecuteAsync(async ct=>{await auth.RequireIdentityAsync(actor,[EmployeeRoles.HrAdministrator],ct);var b=await OwnedAsync(actor,id,ct);if(b.ConfirmationKey!=key)return;var rows=Decode<List<ImportRow>>(b.RowsJson);var i=rows.FindIndex(r=>r.Row==row&&r.State=="Ready");if(i>=0){rows[i]=rows[i] with{State="Failed",Errors=["Persistence failed; no account was committed. Revalidate explicitly before retrying."]};Save(b,rows);}},token);
 /// <inheritdoc />
 public Task<ImportPreview> GetAsync(Guid actor,Guid id,CancellationToken token)=>reads.ExecuteAsync(async ct=>{await auth.RequireIdentityAsync(actor,[EmployeeRoles.HrAdministrator],ct);var preview=View(await OwnedAsync(actor,id,ct));
   var ids=preview.Rows.Where(r=>r.EmployeeId.HasValue).Select(r=>r.EmployeeId!.Value).ToArray();
   var accounts=await (from e in db.Employees.AsNoTracking() where ids.Contains(e.Id)
       join u in db.Users.AsNoTracking() on e.IdentityUserId equals u.Id.ToString()
       select new { e.Id, e.IsActive, u.RequiresActivation, u.ActivationExpiresAtUtc, u.InvitationDeliveryState }).ToListAsync(ct);
   var states=accounts.ToDictionary(x=>x.Id,x=>x.RequiresActivation&&!x.IsActive?"Unavailable":x.RequiresActivation&&x.ActivationExpiresAtUtc<=DateTime.UtcNow?"Expired":x.InvitationDeliveryState);
   return preview with { Rows=preview.Rows.Select(r=>r.EmployeeId is {} employee&&states.TryGetValue(employee,out var current)?r with { DeliveryState=current }:r).ToArray() };},token);
 /// <inheritdoc />
 public async Task<ImportPreview> CancelAsync(Guid actor,Guid id,CancellationToken token){ImportPreview? result=null;await writes.ExecuteAsync(async ct=>{await auth.RequireIdentityAsync(actor,[EmployeeRoles.HrAdministrator],ct);var b=await OwnedAsync(actor,id,ct);b.Cancelled=true;b.Version=Guid.NewGuid();result=View(b);},token);return result!;}
 /// <inheritdoc />
 public async Task<byte[]> ResultsAsync(Guid actor,Guid id,CancellationToken token)
 {
  var preview=await GetAsync(actor,id,token);using var writer=new StringWriter(CultureInfo.InvariantCulture);using(var csv=new CsvWriter(writer,CultureInfo.InvariantCulture)){csv.WriteField("Row");csv.WriteField("State");csv.WriteField("EmployeeId");csv.WriteField("DeliveryState");csv.NextRecord();foreach(var r in preview.Rows){csv.WriteField(r.Row);csv.WriteField(r.State);csv.WriteField(r.EmployeeId?.ToString()??"");csv.WriteField(r.DeliveryState??"");csv.NextRecord();}}return Encoding.UTF8.GetBytes(writer.ToString());
 }
 /// <inheritdoc />
 public Task<IReadOnlyList<ImportTemplateDto>> TemplatesAsync(Guid actor,CancellationToken token)=>reads.ExecuteAsync<IReadOnlyList<ImportTemplateDto>>(async ct=>{await auth.RequireIdentityAsync(actor,[EmployeeRoles.HrAdministrator],ct);return (await db.OnboardingScheduleTemplates.AsNoTracking().OrderBy(t=>t.Code).ToListAsync(ct)).Select(t=>new ImportTemplateDto(t.Id,t.Code,t.Name,Decode<int[]>(t.MinutesJson),t.Version)).ToArray();},token);
 /// <inheritdoc />
 public async Task<ImportTemplateDto> SaveTemplateAsync(Guid actor,ImportTemplateInput input,CancellationToken token)
 {
  ImportTemplateDto? result=null;await writes.ExecuteAsync(async ct=>{
   var hr=await auth.RequireIdentityAsync(actor,[EmployeeRoles.HrAdministrator],ct);var code=EmploymentFacts.NormalizeNumber(input.Code);
   if(input.Minutes is null)throw new DomainException("Supply seven weekday minute values.");
   _=WeeklyScheduleRevision.Create(hr.Id,hr.Id,input.Name,new DateOnly(2000,1,1),input.Minutes);
   var t=await db.OnboardingScheduleTemplates.SingleOrDefaultAsync(t=>t.Code==code,ct);
   if(t is null){if(input.ExpectedVersion.HasValue)throw new WriteConflictException("Template was not found; reload.");t=new(){Code=code};db.OnboardingScheduleTemplates.Add(t);}
   else if(t.Version!=input.ExpectedVersion)throw new WriteConflictException("Template changed; explicitly reload before editing.");
   t.Name=input.Name.Trim();t.MinutesJson=Encode(input.Minutes);t.Version=Guid.NewGuid();result=new(t.Id,t.Code,t.Name,input.Minutes,t.Version);
  },token);return result!;
 }
}
