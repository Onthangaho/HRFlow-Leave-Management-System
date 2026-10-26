using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using HRFlow.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRFlow.RegressionTests;

/// <summary>Real bearer middleware and rotating refresh HTTP gates, with persisted outcomes in migrated private SQLite.</summary>
public sealed class TokenRefreshTests
{
    /// <summary>Wide timing margins exercise skew on the unmodified middleware clock without sleeping near equality.</summary>
    [Fact]
    public async Task BearerLifetimeSigningAndClaimMatrix()
    {
        await using var fixture = await ApiFixture.StartAsync();
        var login = await fixture.LoginAsync("employee");
        var issued = new JwtSecurityTokenHandler().ReadJwtToken(login);
        Assert.InRange((issued.ValidTo-issued.ValidFrom).TotalSeconds, 899, 901);
        using var valid = await fixture.SendAsync(login, "me"); Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
        await using var db = fixture.Context(); var refreshCount = await db.RefreshTokens.CountAsync();
        var now = DateTime.UtcNow; var watch = Stopwatch.StartNew();
        var cases = new[] {
            ("valid", TokenTestTokens.Create(fixture,"employee"), 200),
            ("expiry-inside-skew", TokenTestTokens.Create(fixture,"employee",expires:now.AddSeconds(-10)),200),
            ("expired", TokenTestTokens.Create(fixture,"employee",expires:now.AddSeconds(-45)),401),
            ("not-before-inside-skew", TokenTestTokens.Create(fixture,"employee",notBefore:now.AddSeconds(10)),200),
            ("future-not-before", TokenTestTokens.Create(fixture,"employee",notBefore:now.AddSeconds(45)),401),
            ("issuer", TokenTestTokens.Create(fixture,"employee",issuer:"Synthetic.Wrong"),401),
            ("audience", TokenTestTokens.Create(fixture,"employee",audience:"Synthetic.Wrong"),401),
            ("signature", TokenTestTokens.Create(fixture,"employee",wrongKey:true),401),
            ("algorithm", TokenTestTokens.Create(fixture,"employee",algorithm:"HS384"),401),
            ("unsigned", TokenTestTokens.Create(fixture,"employee",algorithm:"none"),401),
            ("missing-expiry", TokenTestTokens.Create(fixture,"employee",omitExpiry:true),401),
            ("missing-version", TokenTestTokens.Create(fixture,"employee",omitVersion:true),401),
            ("stale-version", TokenTestTokens.Create(fixture,"employee",staleVersion:true),401) };
        foreach (var (label, token, expected) in cases)
        {
            using var response = await fixture.SendAsync(token, "me");
            Assert.True((int)response.StatusCode == expected, "Bearer case: " + label);
        }
        // Fail rather than silently cross a timing boundary on an overloaded host. No retries or longer sleeps.
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10), "Lifetime probes exceeded their explicit timing margin.");
        Assert.Equal(refreshCount, await db.RefreshTokens.CountAsync());
        Assert.Empty(await db.LeaveRequests.ToListAsync()); Assert.Empty(await db.AuditEntries.ToListAsync());
    }

    /// <summary>Two independent API processes exchange one login-issued refresh credential; only one rotation commits.</summary>
    [Fact]
    public async Task ConcurrentRefreshRotatesOnceAndReplayFails()
    {
        await using var fixture = await ApiFixture.StartAsync(); var pair = await PairAsync(fixture,"employee");
        using var connection = fixture.Connection(); using var writer = connection.BeginTransaction(deferred:false);
        var entered = fixture.EnteredAsync("Refresh",2);
        var first = ExchangeAsync(fixture,pair,0); var second = ExchangeAsync(fixture,pair,1);
        await entered; writer.Commit();
        using var a = await first; using var b = await second;
        Assert.Equal(new[] {200,401}, new[] {(int)a.StatusCode,(int)b.StatusCode}.Order().ToArray());
        var next = await ApiFixture.JsonAsync(a.IsSuccessStatusCode ? a : b);
        using var access = await fixture.SendAsync(next["accessToken"]!.GetValue<string>(),"me"); Assert.Equal(HttpStatusCode.OK,access.StatusCode);
        using var replay = await ExchangeAsync(fixture,pair); Assert.Equal(HttpStatusCode.Unauthorized,replay.StatusCode);
        await using var db=fixture.Context(); var rows=await db.RefreshTokens.Where(r=>r.UserId==fixture.User("employee").Id.ToString()).ToArrayAsync();
        Assert.Equal(2,rows.Length); Assert.Single(rows,r=>r.RevokedAtUtc!=null); Assert.Single(rows,r=>r.RevokedAtUtc==null);
        Assert.All(rows,r=>Assert.Equal(TimeSpan.FromDays(7),r.ExpiresAtUtc-r.CreatedAtUtc));
        var jwt=new JwtSecurityTokenHandler().ReadJwtToken(next["accessToken"]!.GetValue<string>());
        Assert.True(jwt.Claims.Any(c=>c.Type==CredentialClaims.Version && c.Value==fixture.User("employee").CredentialVersion.ToString()),"Refreshed token retains current credential generation.");
    }

    /// <summary>Invalid refresh variants never create another pair or business write.</summary>
    [Theory]
    [InlineData("invalid")]
    [InlineData("expired")]
    [InlineData("revoked")]
    public async Task RefreshRefusal(string change)
    {
        await using var fixture=await ApiFixture.StartAsync(); var pair=await PairAsync(fixture,"employee");
        string original;
        await using(var db=fixture.Context())
        {
            var row=await db.RefreshTokens.SingleAsync();
            if(change=="expired")row.ExpiresAtUtc=DateTime.UtcNow.AddMinutes(-5);
            if(change=="revoked")row.RevokedAtUtc=DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
            original=System.Text.Json.JsonSerializer.Serialize(await db.RefreshTokens.AsNoTracking().ToArrayAsync());
        }
        if(change=="invalid")pair["refreshToken"]=Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        using var denied=await ExchangeAsync(fixture,pair); Assert.Equal(HttpStatusCode.Unauthorized,denied.StatusCode);
        await using var check=fixture.Context(); Assert.Single(await check.RefreshTokens.ToArrayAsync());
        Assert.True(original==System.Text.Json.JsonSerializer.Serialize(await check.RefreshTokens.AsNoTracking().ToArrayAsync()),"Refused exchange must not change stored refresh credentials.");
        Assert.Empty(await check.LeaveRequests.ToListAsync()); Assert.Empty(await check.AuditEntries.ToListAsync());
    }

    /// <summary>Refresh uses current roles/lifecycle, and a real password change invalidates both old credential classes.</summary>
    [Theory]
    [InlineData("role")]
    [InlineData("inactive")]
    [InlineData("pending")]
    [InlineData("password")]
    public async Task RevocationRemainsAuthoritative(string change)
    {
        await using var fixture=await ApiFixture.StartAsync(); var pair=await PairAsync(fixture,"combined");
        if(change=="password")
        {
            // Existing private descriptor supplies the generated original password only within this test process.
            var state=System.Text.Json.JsonSerializer.SerializeToNode(fixture.BrowserState)!;
            using var changed=await fixture.SendAsync(pair["accessToken"]!.GetValue<string>(),"auth/password",HttpMethod.Post,
                new{currentPassword=state["password"]!.GetValue<string>(),newPassword="Aa!9"+Convert.ToHexString(RandomNumberGenerator.GetBytes(20))});
            Assert.Equal(HttpStatusCode.NoContent,changed.StatusCode);
        }
        else await using(var db=fixture.Context())
        {
            var user=await db.Users.SingleAsync(u=>u.Id==fixture.User("combined").Id);
            if(change=="pending")user.RequiresActivation=true;
            if(change=="inactive")(await db.Employees.FindAsync(fixture.Employee("combined").Id))!.Deactivate(fixture.Employee("hr").Id,"Synthetic lifecycle");
            if(change=="role") {var role=await db.Roles.SingleAsync(r=>r.Name=="HR Administrator");db.UserRoles.Remove(await db.UserRoles.SingleAsync(r=>r.UserId==user.Id&&r.RoleId==role.Id));}
            await db.SaveChangesAsync();
        }
        using var refreshed=await ExchangeAsync(fixture,pair);
        Assert.Equal(change=="role"?HttpStatusCode.OK:HttpStatusCode.Unauthorized,refreshed.StatusCode);
        if(change=="role")
        {
            var token=(await ApiFixture.JsonAsync(refreshed))["accessToken"]!.GetValue<string>();
            using var forbidden=await fixture.SendAsync(token,"employees"); Assert.Equal(HttpStatusCode.Forbidden,forbidden.StatusCode);
            using var retained=await fixture.SendAsync(token,"leave-requests/dashboard");Assert.Equal(HttpStatusCode.OK,retained.StatusCode);
            using var oldRole=await fixture.SendAsync(pair["accessToken"]!.GetValue<string>(),"employees");Assert.Equal(HttpStatusCode.Forbidden,oldRole.StatusCode);
        }
        else {using var old=await fixture.SendAsync(pair["accessToken"]!.GetValue<string>(),"me");Assert.Equal(HttpStatusCode.Unauthorized,old.StatusCode);}
        await using var check=fixture.Context(); Assert.Empty(await check.LeaveRequests.ToListAsync());Assert.Empty(await check.AuditEntries.ToListAsync());
    }

    private static async Task<JsonNode> PairAsync(ApiFixture fixture,string actor)
    {
        var state=System.Text.Json.JsonSerializer.SerializeToNode(fixture.BrowserState)!;
        using var response=await fixture.SendAsync(null,"auth/login",HttpMethod.Post,new{email=fixture.Employee(actor).Email,password=state["password"]!.GetValue<string>()});
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);return await ApiFixture.JsonAsync(response);
    }
    private static Task<HttpResponseMessage> ExchangeAsync(ApiFixture fixture,JsonNode pair,int server=0)=>fixture.SendAsync(null,"auth/refresh",HttpMethod.Post,new{refreshToken=pair["refreshToken"]!.GetValue<string>()},server);
}
