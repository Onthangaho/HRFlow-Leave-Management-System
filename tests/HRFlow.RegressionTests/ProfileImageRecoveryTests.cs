using HRFlow.Domain.Entities;
using HRFlow.Infrastructure.Persistence;
using HRFlow.Infrastructure.Services;
using HRFlow.Infrastructure.Services.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace HRFlow.RegressionTests;

/// <summary>Offline encrypted recovery checks use generated isolated stores, never a running employee database.</summary>
public sealed class ProfileImageRecoveryTests
{
    [Fact]
    public async Task EncryptedRecoveryPreservesImagesAndRejectsMissingRequiredContent()
    {
        var root = Path.Combine(Path.GetTempPath(), "hrflow-image-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root); ApiFixture.RestrictDirectory(root);
        var database = Path.Combine(root, "fixture.sqlite"); var privateRoot = Path.Combine(root, "private");
        Directory.CreateDirectory(privateRoot); ApiFixture.RestrictDirectory(privateRoot);
        var storage = new PrivateProfileImageStorage(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Storage:PrivateFilesRoot"] = privateRoot }).Build());
        var current = new ProfileImage { Status = "Current" }; var removed = new ProfileImage { Status = "Removed" }; var pending = new ProfileImage();
        var account = new ApplicationUser { Id = Guid.NewGuid(), UserName = "owner@synthetic.invalid", Email = "owner@synthetic.invalid", SecurityStamp = Guid.NewGuid().ToString() };
        var originalCredentialVersion = account.CredentialVersion;
        try
        {
            using var input = new MemoryStream(ProfileImageTests.Raster());
            var output = await storage.ProcessAsync(input, ".png", "image/png", new(0,0,1), default);
            var facts = await storage.PublishAsync(current.Id, output, default); current.Size = facts.Size; current.Checksum = facts.Checksum;
            storage.Remove(removed.Id);
            await using (var db = new HRFlowDbContext(new DbContextOptionsBuilder<HRFlowDbContext>().UseSqlite("Data Source="+database+";Pooling=False").Options))
            {
                await db.Database.MigrateAsync(); var department = new Department { Name = "Synthetic recovery" }; db.Departments.Add(department);
                account.PasswordHash = new Microsoft.AspNetCore.Identity.PasswordHasher<ApplicationUser>().HashPassword(account,
                    Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
                db.Users.Add(account);
                var employee = Employee.Create("Synthetic owner", "owner@synthetic.invalid", department.Id); employee.SetIdentityUser(account.Id.ToString()); db.Employees.Add(employee);
                current.OwnerId = removed.OwnerId = pending.OwnerId = employee.Id; db.ProfileImages.AddRange(current, removed, pending); await db.SaveChangesAsync();
            }
            var key = Path.Combine(root,"private.key"); var package = Path.Combine(root,"backup.package"); var destination = Path.Combine(root,"restored");
            BackupRestoreService.GenerateKey(key, ApiFixture.RepositoryRoot);
            BackupRestoreService.Backup(database, privateRoot, package, key, ApiFixture.RepositoryRoot);
            BackupRestoreService.Restore(package, destination, key, ApiFixture.RepositoryRoot, true);
            await using (var db = new HRFlowDbContext(new DbContextOptionsBuilder<HRFlowDbContext>().UseSqlite("Data Source="+Path.Combine(destination,"database.sqlite")+";Pooling=False").Options))
            {
                var rows = await db.ProfileImages.AsNoTracking().OrderBy(i=>i.Id).ToArrayAsync(); Assert.Equal(3, rows.Length);
                Assert.Equal(current.Checksum, rows.Single(i=>i.Id==current.Id).Checksum); Assert.Equal(current.Version, rows.Single(i=>i.Id==current.Id).Version);
                Assert.Equal("Processing", rows.Single(i=>i.Id==pending.Id).Status); Assert.Equal("Removed", rows.Single(i=>i.Id==removed.Id).Status);
                var recovered = await db.Users.SingleAsync(); Assert.Null(recovered.PasswordHash); Assert.NotEqual(originalCredentialVersion,recovered.CredentialVersion);
                Assert.False(recovered.RequiresActivation); Assert.True((await db.Employees.SingleAsync()).IsActive);
            }
            var restored = new PrivateProfileImageStorage(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Storage:PrivateFilesRoot"] = Path.Combine(destination,"private") }).Build());
            using var verified = await restored.OpenVerifiedAsync(current.Id, current.Size, current.Checksum, default);
            Assert.Equal(output, ((MemoryStream)verified).ToArray());
            await Assert.ThrowsAsync<IOException>(() => restored.PublishAsync(removed.Id, output, default));
            File.Delete(Path.Combine(privateRoot,"profile-images","clean",current.Id.ToString("N")+".blob"));
            Assert.Throws<InvalidDataException>(() => BackupRestoreService.Backup(database, privateRoot, Path.Combine(root,"missing.package"), key, ApiFixture.RepositoryRoot));
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(root,true); }
    }
}
