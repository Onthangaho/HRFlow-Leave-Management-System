using System.Net;
using HRFlow.Application.Interfaces;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Enums;
using HRFlow.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace HRFlow.RegressionTests;

/// <summary>Real HTTP/SQLite binding gates use generated validated blobs with explicitly controlled clean metadata, not claimed malware detection.</summary>
public sealed class EvidenceAdditionTests
{
    internal static async Task<Guid> RequestAsync(ApiFixture f, string owner = "employee", string? mode = RequirementModes.Optional, string classification = SupportingDocumentClass.Ordinary)
    {
        await using var db = f.Context(); var type = await db.LeaveTypes.Include(t => t.LeavePolicy).SingleAsync();
        var request = LeaveRequest.Create(f.Employee(owner).Id, type.Id, DateTime.UtcNow.Date.AddDays(10), DateTime.UtcNow.Date.AddDays(11));
        if (mode != null) request.CaptureRequirements(new(type.Id, type.Version, type.LeavePolicy.Id, type.LeavePolicy.Version,
            RequirementModes.NotRequested, mode, classification, "Synthetic evidence guidance"), null);
        db.LeaveRequests.Add(request); await db.SaveChangesAsync(); return request.Id;
    }
    internal static async Task<SupportingDocument> DraftAsync(ApiFixture f, string owner = "employee", string classification = SupportingDocumentClass.Ordinary)
    {
        var row = new SupportingDocument { OwnerId = f.Employee(owner).Id, UploadKey = Guid.NewGuid(), Class = classification };
        var storage = new PrivateDocumentStorage(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:PrivateFilesRoot"] = f.PrivateRoot }).Build());
        using var bytes = new MemoryStream(ProfileImageTests.Raster());
        var stored = await storage.StageAsync(row.Id, bytes, ".png", "image/png", default);
        row.Size = stored.Size; row.Checksum = stored.Checksum; row.MediaType = stored.MediaType;
        using var verified = await storage.OpenVerifiedAsync(row.Id, false, row.Size, row.Checksum, default);
        storage.Promote(row.Id, verified); row.Status = SupportingDocumentStatus.Clean;
        await using var db = f.Context(); db.SupportingDocuments.Add(row); await db.SaveChangesAsync(); return row;
    }
    private static AddEvidenceRequest Input(params SupportingDocument[] rows) => new(Guid.NewGuid(), rows.Select(r => new EvidenceSelection(r.Id, r.Version)).ToArray());
    private static string PathFor(Guid request) => $"documents/requests/{request}/additions";

    /// <summary>Checks server-confirmed ownership and terminal-state recovery without rewriting captured request facts.</summary>
    [Theory]
    [InlineData("employee", "Optional")]
    [InlineData("manager", "Required")]
    [InlineData("combined", "Optional")]
    public async Task OwnerBindingAndRecoveryPreserveFacts(string actor, string mode)
    {
        await using var f = await ApiFixture.StartAsync(); var request = await RequestAsync(f, actor, mode); var draft = await DraftAsync(f, actor);
        var token = await f.LoginAsync(actor); var input = Input(draft);
        await using var before = f.Context(); var original = await before.LeaveRequests.AsNoTracking().SingleAsync(r => r.Id == request);
        using var added = await f.SendAsync(token, PathFor(request), HttpMethod.Post, input); Assert.Equal(HttpStatusCode.OK, added.StatusCode);
        var outcome = await ApiFixture.JsonAsync(added);
        using var cancelled = await f.SendAsync(token, $"leave-requests/{request}/cancel", HttpMethod.Post, new { }); Assert.Equal(HttpStatusCode.NoContent, cancelled.StatusCode);
        using var recovered = await f.SendAsync(token, PathFor(request), HttpMethod.Post, input, 1); Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
        Assert.Equal(outcome["id"]!.GetValue<Guid>(), (await ApiFixture.JsonAsync(recovered))["id"]!.GetValue<Guid>());
        using var changed = await f.SendAsync(token, PathFor(request), HttpMethod.Post, input with { Documents = [new(draft.Id, Guid.NewGuid())] }); Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
        await using var db = f.Context(); var persisted = await db.LeaveRequests.SingleAsync(r => r.Id == request);
        Assert.Equal(original.SubmissionRequirements, persisted.SubmissionRequirements); Assert.Equal(original.StartDate, persisted.StartDate); Assert.Equal(original.EndDate, persisted.EndDate); Assert.Equal(original.Description, persisted.Description);
        Assert.Single(await db.EvidenceAdditions.ToArrayAsync()); Assert.Single(await db.DocumentAccessEntries.Where(e => e.Action == "SupplementalBind").ToArrayAsync());
        Assert.Equal(2, await db.AuditEntries.CountAsync(e => e.LeaveRequestId == request));
        Assert.Equal(request, (await db.SupportingDocuments.SingleAsync()).RequestId);
        using var download = await f.SendAsync(token, $"documents/{draft.Id}/download"); Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("no-store", download.Headers.CacheControl!.ToString());
    }

    /// <summary>Proves legacy and unsupported captured requirements never become retrospective evidence demands.</summary>
    [Theory]
    [InlineData(null, "Ordinary")]
    [InlineData("NotRequested", "Ordinary")]
    [InlineData("Required", "Medical")]
    public async Task UnsupportedCapturedRequirementsDoNotBind(string? mode, string classification)
    {
        await using var f = await ApiFixture.StartAsync(); var request = await RequestAsync(f, mode: mode, classification: classification); var draft = await DraftAsync(f);
        var token = await f.LoginAsync("employee"); using var response = await f.SendAsync(token, PathFor(request), HttpMethod.Post, Input(draft));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode); await AssertUnbound(f, draft.Id);
    }

    /// <summary>Rejects unsafe or incompatible draft metadata and bytes without partial action history.</summary>
    [Theory]
    [InlineData("Quarantined")]
    [InlineData("Rejected")]
    [InlineData("Missing")]
    [InlineData("version")]
    [InlineData("checksum")]
    [InlineData("missingBlob")]
    [InlineData("owner")]
    [InlineData("bound")]
    [InlineData("duplicate")]
    [InlineData("classification")]
    public async Task InvalidDraftCannotBind(string change)
    {
        await using var f = await ApiFixture.StartAsync(); var request = await RequestAsync(f, classification: change == "classification" ? "Medical" : "Ordinary"); var draft = await DraftAsync(f);
        var input = Input(draft);
        if(change=="missingBlob")
        {
            var storage=new PrivateDocumentStorage(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["Storage:PrivateFilesRoot"]=f.PrivateRoot}).Build());storage.Remove(draft.Id);
        }
        await using (var db = f.Context())
        {
            var row = await db.SupportingDocuments.SingleAsync();
            switch(change)
            {
                case "version": row.Version = Guid.NewGuid(); break;
                case "checksum": row.Checksum = new string('0',64); break;
                case "owner": row.OwnerId = f.Employee("unrelated").Id; break;
                case "bound": row.RequestId = await RequestAsync(f); break;
                case "duplicate": input = input with { Documents = [input.Documents[0], input.Documents[0]] }; break;
                case "classification": case "missingBlob": break;
                default: row.Status = change; break;
            }
            await db.SaveChangesAsync();
        }
        using var response = await f.SendAsync(await f.LoginAsync("employee"), PathFor(request), HttpMethod.Post, input);
        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.Conflict, HttpStatusCode.NotFound, HttpStatusCode.BadRequest });
        await using var state = f.Context(); Assert.False(await state.EvidenceAdditions.AnyAsync()); Assert.False(await state.DocumentAccessEntries.AnyAsync());
        Assert.Equal(1, await state.AuditEntries.CountAsync(a => a.LeaveRequestId == request));
    }

    /// <summary>Checks current read scopes, medical redaction and revocation even for a durable recovery key.</summary>
    [Fact]
    public async Task MedicalPrivacyAndCurrentAccess()
    {
        await using var f = await ApiFixture.StartAsync(); var request = await RequestAsync(f, classification: "Medical"); var draft = await DraftAsync(f, classification: "Medical");
        var owner = await f.LoginAsync("employee"); var input = Input(draft);
        using var added = await f.SendAsync(owner, PathFor(request), HttpMethod.Post, input); Assert.Equal(HttpStatusCode.OK, added.StatusCode);
        foreach(var actor in new[] { "hr", "manager", "unrelated" })
        {
            var token = await f.LoginAsync(actor);
            using var denied = await f.SendAsync(token, PathFor(request), HttpMethod.Post, input); Assert.Contains(denied.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden });
            using var list = await f.SendAsync(token, $"documents?requestId={request}");
            if(actor == "unrelated") { Assert.Equal(HttpStatusCode.NotFound,list.StatusCode); continue; }
            var row = (await ApiFixture.JsonAsync(list)).AsArray().Single()!;
            Assert.Equal(actor == "hr", row["canDownload"]!.GetValue<bool>());
            if(actor == "manager") { Assert.Null(row["size"]); Assert.Null(row["mediaType"]); }
            using var download = await f.SendAsync(token, $"documents/{draft.Id}/download"); Assert.Equal(actor == "hr" ? HttpStatusCode.OK : HttpStatusCode.NotFound,download.StatusCode);
        }
        await using var db = f.Context(); db.UserRoles.RemoveRange(await db.UserRoles.Where(r => r.UserId == f.User("employee").Id).ToArrayAsync()); await db.SaveChangesAsync();
        using var recovery = await f.SendAsync(owner, PathFor(request), HttpMethod.Post, input); Assert.Equal(HttpStatusCode.Forbidden,recovery.StatusCode);
    }

    /// <summary>Coordinates two real API processes to prove the atomic last slot and duplicate-outcome rules.</summary>
    [Fact]
    public async Task LastSlotAndIdenticalRecoveryAcrossProcesses()
    {
        await using var f = await ApiFixture.StartAsync(); var request = await RequestAsync(f); var token = await f.LoginAsync("employee");
        for(var i=0;i<4;i++){var draft=await DraftAsync(f);using var r=await f.SendAsync(token,PathFor(request),HttpMethod.Post,Input(draft));Assert.Equal(HttpStatusCode.OK,r.StatusCode);}
        var a=await DraftAsync(f);var b=await DraftAsync(f);
        using(var connection=f.Connection())using(var writer=connection.BeginTransaction(deferred:false))
        {
            var entered=f.EnteredAsync("DocumentsController.Add",2);
            var first=f.SendAsync(token,PathFor(request),HttpMethod.Post,Input(a),0);var second=f.SendAsync(token,PathFor(request),HttpMethod.Post,Input(b),1);
            await entered;writer.Commit();var results=await Task.WhenAll(first,second);
            Assert.Single(results,r=>r.StatusCode==HttpStatusCode.OK);Assert.Single(results,r=>r.StatusCode==HttpStatusCode.Conflict);foreach(var r in results)r.Dispose();
        }
        await using var db=f.Context();Assert.Equal(5,await db.SupportingDocuments.CountAsync(d=>d.RequestId==request));Assert.Equal(5,await db.EvidenceAdditions.CountAsync());
        var another=await RequestAsync(f);var draft2=await DraftAsync(f);var input=Input(draft2);
        using(var connection=f.Connection())using(var writer=connection.BeginTransaction(deferred:false))
        {
            var entered=f.EnteredAsync("DocumentsController.Add",2);var first=f.SendAsync(token,PathFor(another),HttpMethod.Post,input,0);var second=f.SendAsync(token,PathFor(another),HttpMethod.Post,input,1);
            await entered;writer.Commit();var results=await Task.WhenAll(first,second);Assert.All(results,r=>Assert.Equal(HttpStatusCode.OK,r.StatusCode));foreach(var r in results)r.Dispose();
        }
        Assert.Equal(1,await db.EvidenceAdditions.CountAsync(e=>e.RequestId==another));
    }

    /// <summary>Uses an explicit writer barrier to deny additions after a competing persisted lifecycle change.</summary>
    [Theory]
    [InlineData("Approve")]
    [InlineData("Reject")]
    [InlineData("Cancel")]
    [InlineData("Deactivate")]
    public async Task TransitionWinsWriterRace(string action)
    {
        await using var f=await ApiFixture.StartAsync();var request=await RequestAsync(f);var draft=await DraftAsync(f);var token=await f.LoginAsync("employee");
        using(var connection=f.Connection())using(var writer=connection.BeginTransaction(deferred:false))
        {
            var entered=f.EnteredAsync("DocumentsController.Add");var pending=f.SendAsync(token,PathFor(request),HttpMethod.Post,Input(draft),1);await entered;
            using var command=connection.CreateCommand();command.Transaction=writer;
            command.CommandText=action=="Deactivate"?"UPDATE Employees SET IsActive=0 WHERE Id=$owner":"UPDATE LeaveRequests SET Status=$status WHERE Id=$request";
            command.Parameters.AddWithValue("$owner",f.Employee("employee").Id);command.Parameters.AddWithValue("$request",request);
            command.Parameters.AddWithValue("$status",action=="Approve"?1:action=="Reject"?2:3);command.ExecuteNonQuery();writer.Commit();
            using var response=await pending;Assert.Equal(action=="Deactivate"?HttpStatusCode.Unauthorized:HttpStatusCode.Conflict,response.StatusCode);
        }
        await AssertUnbound(f,draft.Id);
    }

    /// <summary>Injects a late SQLite abort and inspects all binding, action and recovery records before explicit retry.</summary>
    [Fact]
    public async Task LateFailureRollsBackBindingTraceAndOutcome()
    {
        await using var f=await ApiFixture.StartAsync();var request=await RequestAsync(f);var draft=await DraftAsync(f);var input=Input(draft);var token=await f.LoginAsync("employee");
        await using var db=f.Context();await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailAddition AFTER INSERT ON EvidenceAdditions BEGIN SELECT RAISE(ABORT,'synthetic late addition'); END");
        try{using var failed=await f.SendAsync(token,PathFor(request),HttpMethod.Post,input);Assert.Equal(HttpStatusCode.InternalServerError,failed.StatusCode);await AssertUnbound(f,draft.Id);}
        finally{await db.Database.ExecuteSqlRawAsync("DROP TRIGGER FailAddition");}
        using var retry=await f.SendAsync(token,PathFor(request),HttpMethod.Post,input);Assert.Equal(HttpStatusCode.OK,retry.StatusCode);
    }

    /// <summary>Runs real lifecycle APIs after binding and preserves traceability while applying current account access.</summary>
    [Theory]
    [InlineData("approve")]
    [InlineData("reject")]
    [InlineData("cancel")]
    [InlineData("deactivate")]
    public async Task BindingWinsAndRealTransitionPreservesTrace(string action)
    {
        await using var f=await ApiFixture.StartAsync();var request=await RequestAsync(f);var draft=await DraftAsync(f);
        var owner=await f.LoginAsync("employee");var input=Input(draft);
        using(var bind=await f.SendAsync(owner,PathFor(request),HttpMethod.Post,input))Assert.Equal(HttpStatusCode.OK,bind.StatusCode);
        if(action=="deactivate")
        {
            var hr=await f.LoginAsync("hr");using var profile=await f.SendAsync(hr,$"employees/{f.Employee("employee").Id}");
            var version=(await ApiFixture.JsonAsync(profile))["version"]!.GetValue<Guid>();
            using var transition=await f.SendAsync(hr,$"employees/{f.Employee("employee").Id}/deactivate",HttpMethod.Patch,new {expectedVersion=version,reason="Synthetic lifecycle"});Assert.Equal(HttpStatusCode.OK,transition.StatusCode);
            using var denied=await f.SendAsync(owner,PathFor(request),HttpMethod.Post,input);Assert.Equal(HttpStatusCode.Unauthorized,denied.StatusCode);
        }
        else
        {
            using var transition=await f.SendAsync(action=="cancel"?owner:await f.LoginAsync("manager"),$"leave-requests/{request}/{action}",HttpMethod.Post,new {});Assert.Equal(HttpStatusCode.NoContent,transition.StatusCode);
            using var recovery=await f.SendAsync(owner,PathFor(request),HttpMethod.Post,input);Assert.Equal(HttpStatusCode.OK,recovery.StatusCode);
        }
        await using var db=f.Context();Assert.Single(await db.EvidenceAdditions.ToArrayAsync());Assert.Single(await db.DocumentAccessEntries.Where(e=>e.Action=="SupplementalBind").ToArrayAsync());
        Assert.Equal(request,(await db.SupportingDocuments.SingleAsync()).RequestId);Assert.Equal(2,await db.AuditEntries.CountAsync(a=>a.LeaveRequestId==request));
        Assert.NotNull((await db.LeaveRequests.SingleAsync(r=>r.Id==request)).SubmissionRequirements);
    }

    /// <summary>Releases competing live API requests from a shared writer barrier and inspects either valid serial outcome.</summary>
    [Theory]
    [InlineData("approve")]
    [InlineData("reject")]
    [InlineData("cancel")]
    [InlineData("deactivate")]
    public async Task RealCompetingTransitionAcrossProcesses(string action)
    {
        await using var f=await ApiFixture.StartAsync();var request=await RequestAsync(f);var draft=await DraftAsync(f);
        var owner=await f.LoginAsync("employee");var actor=action=="cancel"?owner:await f.LoginAsync(action=="deactivate"?"hr":"manager");
        Guid version=default;
        if(action=="deactivate"){using var profile=await f.SendAsync(actor,$"employees/{f.Employee("employee").Id}");version=(await ApiFixture.JsonAsync(profile))["version"]!.GetValue<Guid>();}
        using(var connection=f.Connection())using(var writer=connection.BeginTransaction(deferred:false))
        {
            var addEntered=f.EnteredAsync("DocumentsController.Add");
            var transitionEntered=f.EnteredAsync(action=="deactivate"?"DeactivateEmployee":action=="approve"?"ApproveLeaveRequest":action=="reject"?"RejectLeaveRequest":"CancelLeaveRequest");
            var addition=f.SendAsync(owner,PathFor(request),HttpMethod.Post,Input(draft),0);
            var transition=action=="deactivate"?f.SendAsync(actor,$"employees/{f.Employee("employee").Id}/deactivate",HttpMethod.Patch,new{expectedVersion=version,reason="Synthetic competing lifecycle"},1)
                :f.SendAsync(actor,$"leave-requests/{request}/{action}",HttpMethod.Post,new{},1);
            await Task.WhenAll(addEntered,transitionEntered);writer.Commit();
            using var bound=await addition;using var changed=await transition;
            Assert.Equal(action=="deactivate"?HttpStatusCode.OK:HttpStatusCode.NoContent,changed.StatusCode);
            Assert.Contains(bound.StatusCode,new[]{HttpStatusCode.OK,HttpStatusCode.Conflict,HttpStatusCode.Unauthorized,HttpStatusCode.Forbidden});
            await using var db=f.Context();var didBind=bound.StatusCode==HttpStatusCode.OK;
            Assert.Equal(didBind?1:0,await db.EvidenceAdditions.CountAsync());Assert.Equal(didBind?1:0,await db.DocumentAccessEntries.CountAsync(e=>e.Action=="SupplementalBind"));
            Assert.Equal(didBind?request:(Guid?)null,(await db.SupportingDocuments.SingleAsync()).RequestId);
            Assert.Equal(2,await db.AuditEntries.CountAsync(a=>a.LeaveRequestId==request));Assert.NotEqual(LeaveRequestStatus.Pending,(await db.LeaveRequests.SingleAsync()).Status);
        }
    }

    /// <summary>Revokes credentials or roles while a live request waits and proves no binding survives.</summary>
    [Theory]
    [InlineData("role")]
    [InlineData("credential")]
    public async Task RevokedActorBehindWriterCannotBind(string change)
    {
        await using var f=await ApiFixture.StartAsync();var request=await RequestAsync(f);var draft=await DraftAsync(f);var token=await f.LoginAsync("employee");
        using(var connection=f.Connection())using(var writer=connection.BeginTransaction(deferred:false))
        {
            var entered=f.EnteredAsync("DocumentsController.Add");var pending=f.SendAsync(token,PathFor(request),HttpMethod.Post,Input(draft));await entered;
            using var command=connection.CreateCommand();command.Transaction=writer;
            command.CommandText=change=="role"?"DELETE FROM AspNetUserRoles WHERE UserId=$user":"UPDATE AspNetUsers SET CredentialVersion=$version WHERE Id=$user";
            command.Parameters.AddWithValue("$user",f.User("employee").Id);command.Parameters.AddWithValue("$version",Guid.NewGuid().ToString("N"));command.ExecuteNonQuery();writer.Commit();
            using var response=await pending;Assert.Equal(change=="role"?HttpStatusCode.Forbidden:HttpStatusCode.Unauthorized,response.StatusCode);
        }
        await AssertUnbound(f,draft.Id);
    }

    /// <summary>Checks the new route cannot retain access through an old active-session token.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InactiveOrPendingAccountCannotAddOrRecover(bool pending)
    {
        await using var f=await ApiFixture.StartAsync();var request=await RequestAsync(f);var draft=await DraftAsync(f);var token=await f.LoginAsync("employee");
        await using var db=f.Context();
        if(pending)(await db.Users.FindAsync(f.User("employee").Id))!.RequiresActivation=true;
        else(await db.Employees.FindAsync(f.Employee("employee").Id))!.Deactivate(f.Employee("hr").Id,"Synthetic inactive");
        await db.SaveChangesAsync();using var response=await f.SendAsync(token,PathFor(request),HttpMethod.Post,Input(draft));Assert.Equal(HttpStatusCode.Unauthorized,response.StatusCode);await AssertUnbound(f,draft.Id);
    }

    /// <summary>Bounds malformed transport before it can create partial provenance or recovery records.</summary>
    [Fact]
    public async Task NullSelectionAndOversizedBodyFailSafely()
    {
        await using var f=await ApiFixture.StartAsync();var request=await RequestAsync(f);var token=await f.LoginAsync("employee");
        using var invalid=await f.SendAsync(token,PathFor(request),HttpMethod.Post,new{key=Guid.NewGuid(),documents=new object?[]{null}});Assert.Equal(HttpStatusCode.BadRequest,invalid.StatusCode);
        using var oversized=await f.SendAsync(token,PathFor(request),HttpMethod.Post,new{key=Guid.NewGuid(),documents=Array.Empty<object>(),padding=new string('x',5000)});Assert.Equal(HttpStatusCode.RequestEntityTooLarge,oversized.StatusCode);
        await using var db=f.Context();Assert.False(await db.EvidenceAdditions.AnyAsync());Assert.False(await db.DocumentAccessEntries.AnyAsync());
    }
    private static async Task AssertUnbound(ApiFixture f,Guid id)
    {
        await using var db=f.Context();Assert.Null((await db.SupportingDocuments.SingleAsync(d=>d.Id==id)).RequestId);
        Assert.False(await db.EvidenceAdditions.AnyAsync());Assert.False(await db.DocumentAccessEntries.AnyAsync());
    }

    /// <summary>Checks explicit private permissions for every document publication area and retained removal marker.</summary>
    [Fact]
    public async Task DocumentPublicationAndRemovalStayOwnerOnly()
    {
        await using var f = await ApiFixture.StartAsync();
        var draft = await DraftAsync(f);
        void AssertStorage()
        {
            ApiFixture.AssertPrivatePath(f.PrivateRoot);
            foreach (var path in Directory.EnumerateFileSystemEntries(f.PrivateRoot, "*", SearchOption.AllDirectories)) ApiFixture.AssertPrivatePath(path);
        }
        AssertStorage();
        var storage = new PrivateDocumentStorage(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:PrivateFilesRoot"] = f.PrivateRoot }).Build());
        storage.Remove(draft.Id);
        AssertStorage();
        using var bytes = new MemoryStream(ProfileImageTests.Raster());
        await Assert.ThrowsAsync<IOException>(() => storage.StageAsync(draft.Id, bytes, ".png", "image/png", default));
        using var verified = new MemoryStream(ProfileImageTests.Raster());
        Assert.Throws<IOException>(() => storage.Promote(draft.Id, verified));
        AssertStorage();
    }

    /// <summary>Uses actual stopped-writer encrypted tools and verifies restored metadata, blobs and security invalidation.</summary>
    [Fact]
    public async Task EncryptedRecoveryPreservesAdditionAndBoundBytes()
    {
        await using var f=await ApiFixture.StartAsync();var request=await RequestAsync(f);var draft=await DraftAsync(f);
        using(var added=await f.SendAsync(await f.LoginAsync("employee"),PathFor(request),HttpMethod.Post,Input(draft)))Assert.Equal(HttpStatusCode.OK,added.StatusCode);
        var authentication=f.AuthenticationEnvironment();await f.StopAsync();
        ApiFixture.AssertPrivatePath(f.DatabasePath);
        ApiFixture.AssertPrivatePath(f.PrivateRoot);
        foreach (var path in Directory.EnumerateFileSystemEntries(f.PrivateRoot, "*", SearchOption.AllDirectories)) ApiFixture.AssertPrivatePath(path);
        await using var drill=await ActivationRecoveryFixture.CreateAsync(authentication);
        await drill.ToolAsync("keygen","--output",drill.Key);
        await drill.ToolAsync("backup","--database",f.DatabasePath,"--private-root",f.PrivateRoot,"--output",drill.Package,"--key-file",drill.Key);
        await drill.ToolAsync("restore","--package",drill.Package,"--destination",drill.Destination,"--key-file",drill.Key,"--invalidate-security");
        await using var restored=ActivationRecoveryFixture.Context(drill.Database);
        var entry=await restored.EvidenceAdditions.SingleAsync();Assert.Equal(request,entry.RequestId);Assert.Equal(f.Employee("employee").Id,entry.ActorId);
        var document=await restored.SupportingDocuments.SingleAsync();Assert.Equal(draft.Checksum,document.Checksum);Assert.Equal(request,document.RequestId);
        var storage=new PrivateDocumentStorage(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["Storage:PrivateFilesRoot"]=System.IO.Path.Combine(drill.Destination,"private")}).Build());
        using var bytes=await storage.OpenVerifiedAsync(document.Id,true,document.Size,document.Checksum,default);Assert.Equal(document.Size,bytes.Length);
        Assert.Single(await restored.AuditEntries.ToArrayAsync());Assert.Single(await restored.DocumentAccessEntries.ToArrayAsync());
        Assert.All(await restored.Users.ToArrayAsync(),u=>Assert.Null(u.PasswordHash));
        drill.AssertPermissions();
    }

    /// <summary>Checks additive schema consistency and immutable requirements despite a later incompatible type edit.</summary>
    [Fact]
    public async Task ModelMatchesMigrationAndLaterTypeChangesDoNotDemandEvidence()
    {
        await using var f=await ApiFixture.StartAsync();await using var db=f.Context();Assert.False(db.Database.HasPendingModelChanges());
        var request=await RequestAsync(f);var type=await db.LeaveTypes.SingleAsync();type.ConfigureRequirements("Required","NotRequested","Medical","Later unrelated instructions");await db.SaveChangesAsync();
        var draft=await DraftAsync(f);using var response=await f.SendAsync(await f.LoginAsync("employee"),PathFor(request),HttpMethod.Post,Input(draft));Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        var persisted=await db.LeaveRequests.AsNoTracking().SingleAsync(r=>r.Id==request);Assert.Equal("Optional",persisted.SubmissionRequirements!.EvidenceMode);Assert.Equal("Ordinary",persisted.SubmissionRequirements.EvidenceClass);
    }

    /// <summary>Real required-evidence submission and later addition share the same five-file model without extra transition audits.</summary>
    [Fact]
    public async Task RequiredOrdinaryInitialSubmissionThenSupplement()
    {
        await using var f=await ApiFixture.StartAsync();await using(var db=f.Context()){var type=await db.LeaveTypes.SingleAsync();type.ConfigureRequirements("NotRequested","Required","Ordinary","Synthetic ordinary proof");await db.SaveChangesAsync();}
        var first=await DraftAsync(f);var token=await f.LoginAsync("employee");using var selector=await f.SendAsync(token,"leave-types");var selected=(await ApiFixture.JsonAsync(selector)).AsArray().Single()!;
        using var submitted=await f.SendAsync(token,"leave-requests",HttpMethod.Post,new{leaveTypeId=selected["id"]!.GetValue<Guid>(),startDate=DateTime.UtcNow.Date.AddDays(12),endDate=DateTime.UtcNow.Date.AddDays(12),expectedTypeVersion=selected["version"]!.GetValue<Guid>(),expectedPolicyVersion=selected["policyVersion"]!.GetValue<Guid>(),documentIds=new[]{first.Id}});
        Assert.Equal(HttpStatusCode.OK,submitted.StatusCode);var request=(await ApiFixture.JsonAsync(submitted)).GetValue<Guid>();var second=await DraftAsync(f);
        using var added=await f.SendAsync(token,PathFor(request),HttpMethod.Post,Input(second));Assert.Equal(HttpStatusCode.OK,added.StatusCode);
        await using var state=f.Context();Assert.Equal(2,await state.SupportingDocuments.CountAsync(d=>d.RequestId==request));Assert.Single(await state.AuditEntries.ToArrayAsync());Assert.Single(await state.EvidenceAdditions.ToArrayAsync());
        Assert.Equal("Required",(await state.LeaveRequests.SingleAsync()).SubmissionRequirements!.EvidenceMode);
    }
}
