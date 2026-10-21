using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HRFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRFlow.RegressionTests;

/// <summary>Joins real encrypted recovery, normal API startup, live authorization and private SMTP observations in one sequential lineage.</summary>
public sealed class ActivationRecoveryTests
{
    /// <summary>Restoring data never restores invitation authority; only reviewed account recovery and an explicit fresh resend establish access.</summary>
    [Fact]
    public async Task RestoredStoreInvalidatesOldAuthorityAndOnlyExplicitResendDispatches()
    {
        await using var api = await ApiFixture.StartAsync();
        var auth = api.AuthenticationEnvironment();
        var originalPassword = JsonSerializer.SerializeToNode(api.BrowserState)!["password"]!.GetValue<string>();
        var oldCredentials = new List<(string Email, string Access, string Refresh, string Password)>();
        foreach (var actor in new[] { "hr", "employee", "manager", "combined", "unrelated" })
        {
            using var response = await api.SendAsync(null, "auth/login", HttpMethod.Post, new { email = api.Employee(actor).Email, password = originalPassword });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode); var pair = await ApiFixture.JsonAsync(response);
            oldCredentials.Add((api.Employee(actor).Email, pair["accessToken"]!.GetValue<string>(), pair["refreshToken"]!.GetValue<string>(), originalPassword));
        }
        var request = await api.SubmitAsync("employee", 10, 2);
        using (var approved = await api.SendAsync(await api.LoginAsync("manager"), $"leave-requests/{request}/approve", HttpMethod.Post, new { note = "Synthetic recovery decision" })) Assert.Equal(HttpStatusCode.NoContent, approved.StatusCode);
        await api.StopAsync();
        await using var drill = await ActivationRecoveryFixture.CreateAsync(auth);
        drill.Secrets.AddRange(new[] { api.DatabasePath, api.PrivateRoot });
        drill.Secrets.Add(originalPassword); drill.Secrets.AddRange(oldCredentials.SelectMany(p => new[] { p.Access, p.Refresh }));
        var original = await drill.StartHostAsync(api.DatabasePath, api.PrivateRoot);
        var hr = await original.LoginAsync(api.Employee("hr").Email, originalPassword); drill.Secrets.Add(hr);
        var oldInvitations = new List<string>();
        async Task<(Guid Employee, string Token)> CreateAsync(string name, string expected)
        {
            using var response = await original.SendAsync(hr, "employees", HttpMethod.Post, api.Creation(name + "@synthetic.invalid", "REC-" + name.ToUpperInvariant()));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode); var data = await ApiFixture.JsonAsync(response);
            Assert.Equal(expected, data["invitationDeliveryState"]!.GetValue<string>());
            var token = PrivateActivationSmtp.Invitation(await drill.Smtp.MessageAsync()); oldInvitations.Add(token); drill.Secrets.Add(token);
            return (data["employeeId"]!.GetValue<Guid>(), token);
        }
        var accepted = await CreateAsync("accepted", "Accepted");
        drill.Smtp.Mode = "disconnect"; _ = await CreateAsync("uncertain", "DeliveryUnknown"); drill.Smtp.Mode = "accept";
        drill.Smtp.AfterMessage = async () => { await using var db = api.Context(); await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER RecoveryLostAck BEFORE UPDATE OF InvitationDeliveryState ON AspNetUsers BEGIN SELECT RAISE(ABORT,'synthetic recovery acknowledgement'); END"); };
        _ = await CreateAsync("pending", "PendingDelivery"); drill.Smtp.AfterMessage = null;
        await using (var db = api.Context()) await db.Database.ExecuteSqlRawAsync("DROP TRIGGER RecoveryLostAck");
        var activated = await CreateAsync("activated", "Accepted"); var activatedPassword = "Aa!9" + Guid.NewGuid().ToString("N"); drill.Secrets.Add(activatedPassword);
        using (var response = await original.SendAsync(null, "auth/activation", HttpMethod.Post, new { token = activated.Token, password = activatedPassword })) Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using (var signedIn = await original.SendAsync(null, "auth/login", HttpMethod.Post, new { email = "activated@synthetic.invalid", password = activatedPassword }))
        {
            Assert.Equal(HttpStatusCode.OK, signedIn.StatusCode); var pair = await ApiFixture.JsonAsync(signedIn);
            var access = pair["accessToken"]!.GetValue<string>(); var refresh = pair["refreshToken"]!.GetValue<string>();
            oldCredentials.Add(("activated@synthetic.invalid", access, refresh, activatedPassword)); drill.Secrets.AddRange(new[] { access, refresh });
        }
        var columns = new[] { "FirstName", "LastName", "EmailAddress", "EmployeeNumber", "Roles", "Department", "EmploymentStartDate" };
        using var form = new MultipartFormDataContent(); var csv = new ByteArrayContent(Encoding.UTF8.GetBytes(string.Join(',', columns) + "\nSynthetic,CSV,csv-recovery@synthetic.invalid,REC-CSV,Employee,Synthetic Engineering,2020-01-01\n"));
        csv.Headers.ContentType = new MediaTypeHeaderValue("text/csv"); form.Add(csv, "file", "synthetic.csv"); form.Add(new StringContent(JsonSerializer.Serialize(columns.ToDictionary(c => c, c => c))), "mapping");
        using var upload = new HttpRequestMessage(HttpMethod.Post, "api/v1/onboarding/previews") { Content = form }; upload.Headers.Authorization = new("Bearer", hr);
        using var previewResponse = await original.Client.SendAsync(upload); Assert.Equal(HttpStatusCode.Created, previewResponse.StatusCode); var preview = await ApiFixture.JsonAsync(previewResponse);
        var batchId = preview["id"]!.GetValue<Guid>(); var key = Guid.NewGuid().ToString();
        using var confirmation = await original.SendAsync(hr, $"onboarding/previews/{batchId}/confirm", HttpMethod.Post, new { expectedVersion = preview["version"]!.GetValue<Guid>(), idempotencyKey = key, rows = new[] { 2 } });
        Assert.Equal(HttpStatusCode.OK, confirmation.StatusCode); var confirmed = await ApiFixture.JsonAsync(confirmation); Assert.Equal("Created", confirmed["rows"]![0]!["state"]!.GetValue<string>());
        var csvToken = PrivateActivationSmtp.Invitation(await drill.Smtp.MessageAsync()); oldInvitations.Add(csvToken); drill.Secrets.Add(csvToken);
        await original.StopAsync(); Assert.Equal(5, drill.Smtp.Attempts); Assert.Equal(5, drill.Smtp.Submissions);
        Guid sentinel; string preserved; Dictionary<Guid, Guid> versions;
        await using (var db = api.Context())
        {
            var inactive = await db.Employees.SingleAsync(e => e.Id == api.Employee("unrelated").Id); inactive.Deactivate(api.Employee("hr").Id, "Synthetic recovery lifecycle");
            db.AccountSettings.Add(AccountSettings.Create(api.Employee("hr").Id));
            var source = new LeaveNotificationEvent { EventKey = "synthetic-recovery-barrier-" + Guid.NewGuid().ToString("N"), RequestId = request, RecipientId = api.Employee("manager").Id, Kind = "Submit", CreatedAtUtc = DateTime.UtcNow };
            db.LeaveNotificationEvents.Add(source); sentinel = source.Id; drill.WorkerSentinel = sentinel; await db.SaveChangesAsync();
            var states = await db.Users.Where(u => u.Email!.EndsWith("@synthetic.invalid") && u.Email != "combined@synthetic.invalid" && u.Email != "unrelated@synthetic.invalid" && u.Email != "cross@synthetic.invalid").Select(u => u.InvitationDeliveryState).ToArrayAsync();
            Assert.Contains("Accepted", states); Assert.Contains("DeliveryUnknown", states); Assert.Contains("PendingDelivery", states); Assert.Contains("Activated", states);
            versions = await db.Users.ToDictionaryAsync(u => u.Id, u => u.CredentialVersion); preserved = await PreservedAsync(db);
        }
        // All original writers are stopped before the real CLI proves quiescence/lease and snapshots.
        Console.WriteLine("recovery-drill: original writers stopped; accounts=12; smtp-attempts=5");
        await drill.ToolAsync("keygen", "--output", drill.Key);
        await drill.ToolAsync("backup", "--database", api.DatabasePath, "--private-root", api.PrivateRoot, "--output", drill.Package, "--key-file", drill.Key);
        await drill.ToolAsync("restore", "--package", drill.Package, "--destination", drill.Destination, "--key-file", drill.Key, "--invalidate-security");
        drill.AssertPermissions();
        Console.WriteLine("recovery-drill: encrypted CLI backup and isolated restore completed");
        await using (var db = ActivationRecoveryFixture.Context(drill.Database))
        {
            Assert.True(preserved == await PreservedAsync(db), "Recovery must preserve roles, profiles, history and durable outcomes.");
            var users = await db.Users.ToArrayAsync(); Assert.Equal(12, users.Length);
            foreach (var user in users)
            {
                Assert.True(user.PasswordHash is null && user.ActivationTokenHash is null && user.ActivationExpiresAtUtc is null
                    && user.ActivationRecipientNormalizedEmail is null && user.ActivationDispatchStartedAtUtc is null,
                    "Restore must clear credentials and all invitation authority without exposing their values.");
                Assert.True(versions[user.Id] != user.CredentialVersion, "Recovery rotates each credential version.");
                if (user.RequiresActivation) Assert.Equal("Expired", user.InvitationDeliveryState);
            }
            var refreshRows = await db.RefreshTokens.ToArrayAsync();
            Assert.True(refreshRows.Length >= oldCredentials.Count);
            Assert.All(refreshRows, row => Assert.NotNull(row.RevokedAtUtc));
            var batch = await db.OnboardingBatches.SingleAsync(b => b.Id == batchId); Assert.True(batch.Cancelled); Assert.True(batch.ExpiresAtUtc <= DateTime.UtcNow);
        }
        await using (var source = api.Context())
        {
            Assert.True(preserved == await PreservedAsync(source), "Isolated restore must not rewrite the source store.");
            Assert.True((await source.Users.ToArrayAsync()).All(u => u.CredentialVersion == versions[u.Id]), "Source credential versions must remain unchanged.");
            Assert.True((await source.Users.SingleAsync(u => u.Id == api.User("hr").Id)).PasswordHash is not null, "Source credentials must not be invalidated.");
        }
        // The sink is already listening, so startup or worker auto-dispatch would be observable.
        var restored = await drill.StartHostAsync(drill.Database, Path.Combine(drill.Destination, "private"));
        await restored.WorkerCommit.Task.WaitAsync(TimeSpan.FromSeconds(15));
        Assert.Equal(5, drill.Smtp.Attempts);
        foreach (var pair in oldCredentials)
        {
            Assert.True(new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(pair.Access).ValidTo > DateTime.UtcNow,
                "Old access credentials must still be unexpired when revocation is checked.");
            using var access = await restored.SendAsync(pair.Access, "me"); Assert.Equal(HttpStatusCode.Unauthorized, access.StatusCode);
            using var refresh = await restored.SendAsync(null, "auth/refresh", HttpMethod.Post, new { refreshToken = pair.Refresh }); Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
            using var login = await restored.SendAsync(null, "auth/login", HttpMethod.Post, new { email = pair.Email, password = pair.Password }); Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        }
        foreach (var token in oldInvitations)
        { using var denial = await restored.SendAsync(null, "auth/activation", HttpMethod.Post, new { token, password = activatedPassword }); Assert.Equal(HttpStatusCode.BadRequest, denial.StatusCode); }
        Console.WriteLine("recovery-drill: persisted invalidation, worker completion and old HTTP authority denied");
        await restored.StopAsync();
        var recoveredPassword = "Aa!9" + Guid.NewGuid().ToString("N"); drill.Secrets.Add(recoveredPassword);
        ApiFixture.CreatePrivateFile(drill.PasswordFile); await File.WriteAllTextAsync(drill.PasswordFile, recoveredPassword);
        await drill.ToolAsync("recover-password", "--database", drill.Database, "--identity", api.User("hr").Id.ToString(), "--password-file", drill.PasswordFile, "--confirm-current-permissions");
        File.Delete(drill.PasswordFile); Assert.False(File.Exists(drill.PasswordFile));
        var production = await drill.StartHostAsync(drill.Database, Path.Combine(drill.Destination, "private"), production: true);
        var productionHr = await production.LoginAsync(api.Employee("hr").Email, recoveredPassword); drill.Secrets.Add(productionHr);
        using (var denied = await production.SendAsync(productionHr, "employees", HttpMethod.Post, api.Creation("production-denied@synthetic.invalid", "REC-PROD"))) Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        using (var denied = await production.SendAsync(productionHr, $"employees/{accepted.Employee}/activation/resend", HttpMethod.Post, new { expectedVersion = Guid.NewGuid() })) Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        using (var denied = await production.SendAsync(productionHr, $"onboarding/previews/{batchId}/confirm", HttpMethod.Post, new { expectedVersion = confirmed["version"]!.GetValue<Guid>(), idempotencyKey = key, rows = new[] { 2 } })) Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        await production.StopAsync();
        _ = await drill.StartHostAsync(drill.Database, Path.Combine(drill.Destination, "private"), production: true, forbiddenPickup: true);
        var reviewed = await drill.StartHostAsync(drill.Database, Path.Combine(drill.Destination, "private"));
        var reviewedHr = await reviewed.LoginAsync(api.Employee("hr").Email, recoveredPassword); drill.Secrets.Add(reviewedHr);
        foreach (var id in new[] { accepted.Employee, activated.Employee })
        { using var status = await reviewed.SendAsync(reviewedHr, $"employees/{id}"); Assert.Equal(HttpStatusCode.OK, status.StatusCode); }
        using (var directory = await reviewed.SendAsync(reviewedHr, "employees?page=1&pageSize=25")) Assert.Equal(HttpStatusCode.OK, directory.StatusCode);
        using (var batch = await reviewed.SendAsync(reviewedHr, $"onboarding/previews/{batchId}"))
        {
            Assert.Equal(HttpStatusCode.OK, batch.StatusCode);
            Assert.Equal("Created", (await ApiFixture.JsonAsync(batch))["rows"]![0]!["state"]!.GetValue<string>());
        }
        using (var oldLogin = await reviewed.SendAsync(null, "auth/login", HttpMethod.Post, new { email = api.Employee("hr").Email, password = originalPassword })) Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
        for (var i = 0; i < 2; i++)
        { using var repeat = await reviewed.SendAsync(reviewedHr, $"onboarding/previews/{batchId}/confirm", HttpMethod.Post, new { expectedVersion = confirmed["version"]!.GetValue<Guid>(), idempotencyKey = key, rows = new[] { 2 } }); Assert.Equal(HttpStatusCode.Conflict, repeat.StatusCode); }
        using (var history = await reviewed.SendAsync(reviewedHr, $"leave-requests/{request}/timeline")) Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        using (var personal = await reviewed.SendAsync(reviewedHr, "leave-requests/dashboard")) Assert.Equal(HttpStatusCode.Forbidden, personal.StatusCode);
        using (var decision = await reviewed.SendAsync(reviewedHr, $"leave-requests/{request}/approve", HttpMethod.Post, new { })) Assert.Equal(HttpStatusCode.Forbidden, decision.StatusCode);
        await using (var db = ActivationRecoveryFixture.Context(drill.Database))
        {
            Assert.True(preserved == await PreservedAsync(db), "Recovery must preserve roles, profiles, history and durable outcomes."); Assert.Equal(12, await db.Users.CountAsync());
            Assert.Equal(1, await db.LeaveNotifications.CountAsync(n => n.EventId == sentinel)); Assert.NotNull((await db.LeaveNotificationEvents.SingleAsync(e => e.Id == sentinel)).DeliveredAtUtc);
            Assert.Equal(new[] { "HR Administrator" }, await RolesAsync(db, api.User("hr").Id));
        }
        Assert.Equal(5, drill.Smtp.Attempts);
        using var detail = await reviewed.SendAsync(reviewedHr, $"employees/{accepted.Employee}"); Assert.Equal(HttpStatusCode.OK, detail.StatusCode); var version = (await ApiFixture.JsonAsync(detail))["version"]!.GetValue<Guid>();
        using var resent = await reviewed.SendAsync(reviewedHr, $"employees/{accepted.Employee}/activation/resend", HttpMethod.Post, new { expectedVersion = version }); Assert.Equal(HttpStatusCode.OK, resent.StatusCode);
        Assert.Equal("Accepted", (await ApiFixture.JsonAsync(resent))["invitationDeliveryState"]!.GetValue<string>());
        Assert.Equal(6, drill.Smtp.Attempts); Assert.Equal(6, drill.Smtp.Submissions);
        var fresh = PrivateActivationSmtp.Invitation(await drill.Smtp.MessageAsync()); drill.Secrets.Add(fresh); Assert.True(oldInvitations.All(t => t != fresh), "Resend must rotate invitation authority.");
        foreach (var token in oldInvitations)
        { using var denied = await reviewed.SendAsync(null, "auth/activation", HttpMethod.Post, new { token, password = activatedPassword }); Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode); }
        using (var redeemed = await reviewed.SendAsync(null, "auth/activation", HttpMethod.Post, new { token = fresh, password = activatedPassword })) Assert.Equal(HttpStatusCode.NoContent, redeemed.StatusCode);
        using (var duplicate = await reviewed.SendAsync(null, "auth/activation", HttpMethod.Post, new { token = fresh, password = activatedPassword })) Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        _ = await reviewed.LoginAsync("accepted@synthetic.invalid", activatedPassword);
        Console.WriteLine("recovery-drill: reviewed HR recovery and explicit resend only; smtp-attempts=6; redemption single-use");
        await reviewed.StopAsync();
        var restarted = await drill.StartHostAsync(drill.Database, Path.Combine(drill.Destination, "private"));
        using (var old = await restarted.SendAsync(oldCredentials[0].Access, "me")) Assert.Equal(HttpStatusCode.Unauthorized, old.StatusCode);
        Assert.Equal(6, drill.Smtp.Attempts);
        await restarted.StopAsync();
        await using (var final = ActivationRecoveryFixture.Context(drill.Database))
        {
            var account = await final.Users.SingleAsync(u => u.Email == "accepted@synthetic.invalid"); Assert.False(account.RequiresActivation); Assert.True(account.ActivationTokenHash is null, "Successful activation consumes the new invitation hash.");
            Assert.Equal(new[] { "Employee" }, await RolesAsync(final, account.Id)); Assert.Equal(12, await final.Users.CountAsync());
            Assert.Equal(2, await final.AuditEntries.CountAsync(a => a.LeaveRequestId == request));
            Assert.Equal(1, await final.LeaveNotifications.CountAsync(n => n.EventId == sentinel));
        }
        drill.AssertPermissions();
        Console.WriteLine("recovery-drill: restart preserved account/outcome/audit counts; no extra transport");
    }
    private static Task<string?[]> RolesAsync(HRFlow.Infrastructure.Persistence.HRFlowDbContext db, Guid user) =>
        (from ur in db.UserRoles join role in db.Roles on ur.RoleId equals role.Id where ur.UserId == user orderby role.Name select role.Name).ToArrayAsync();
    private static async Task<string> PreservedAsync(HRFlow.Infrastructure.Persistence.HRFlowDbContext db)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            Employees = await db.Employees.AsNoTracking().OrderBy(e => e.Id).ToArrayAsync(),
            Accounts = await db.Users.AsNoTracking().OrderBy(u => u.Id).Select(u => new { u.Id, u.Email, u.NormalizedEmail, u.UserName, u.RequiresActivation, u.ActivatedAtUtc }).ToArrayAsync(),
            Roles = await db.UserRoles.AsNoTracking().OrderBy(r => r.UserId).ThenBy(r => r.RoleId).ToArrayAsync(),
            RoleDefinitions = await db.Roles.AsNoTracking().OrderBy(r => r.Id).ToArrayAsync(),
            Requests = await db.LeaveRequests.AsNoTracking().OrderBy(r => r.Id).ToArrayAsync(),
            Audits = await db.AuditEntries.AsNoTracking().OrderBy(a => a.Id).ToArrayAsync(),
            Settings = await db.AccountSettings.AsNoTracking().OrderBy(s => s.EmployeeId).ToArrayAsync(),
            Outcomes = await db.OnboardingBatches.AsNoTracking().OrderBy(b => b.Id).Select(b => new { b.Id, b.ActorId, b.RowsJson, b.ConfirmationKey, b.SelectedRowsJson }).ToArrayAsync(),
            Events = await db.LeaveNotificationEvents.AsNoTracking().OrderBy(e => e.Id).Select(e => new { e.Id, e.EventKey, e.RecipientId, e.RequestId, e.Kind, e.CreatedAtUtc }).ToArrayAsync()
        }); return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
