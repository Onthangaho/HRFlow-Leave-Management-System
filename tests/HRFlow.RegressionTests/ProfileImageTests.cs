using HRFlow.Application.Interfaces;
using HRFlow.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using SkiaSharp;
using Xunit;

namespace HRFlow.RegressionTests;

/// <summary>Bounded image fixtures contain only generated colour blocks; no personal photos or scanner substitute.</summary>
public sealed class ProfileImageTests
{
    private static PrivateProfileImageStorage Storage(string? root = null) => new(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Storage:PrivateFilesRoot"] = root }).Build());

    /// <summary>Generates valid synthetic rasters using the same installed encoder, not committed binary uploads.</summary>
    public static byte[] Raster(SKEncodedImageFormat format = SKEncodedImageFormat.Png)
    {
        using var bitmap = new SKBitmap(80, 40);
        using (var canvas = new SKCanvas(bitmap)) { canvas.Clear(SKColors.CornflowerBlue); }
        using var image = SKImage.FromBitmap(bitmap); using var data = image.Encode(format, 95); return data.ToArray();
    }

    [Theory]
    [InlineData(".png", "image/png", SKEncodedImageFormat.Png)]
    [InlineData(".jpg", "image/jpeg", SKEncodedImageFormat.Jpeg)]
    public async Task ReencodesBoundedPixelsWithoutSourceMetadata(string extension, string mime, SKEncodedImageFormat format)
    {
        var bytes = Raster(format);
        using var input = new MemoryStream(bytes);
        var output = await Storage().ProcessAsync(input, extension, mime, new(.25, 0, 1), default);
        using var data = SKData.CreateCopy(output); using var codec = SKCodec.Create(data);
        Assert.Equal(256, codec.Info.Width); Assert.Equal(256, codec.Info.Height);
        Assert.Equal(SKEncodedImageFormat.Png, codec.EncodedFormat);
        Assert.Equal(SKEncodedOrigin.TopLeft, codec.EncodedOrigin);
        Assert.NotEqual(bytes, output);
    }

    [Theory]
    [InlineData(SKEncodedImageFormat.Jpeg)]
    [InlineData(SKEncodedImageFormat.Png)]
    public async Task ExifRotationIsAppliedAndMetadataIsStripped(SKEncodedImageFormat format)
    {
        using var source = new SKBitmap(80, 40);
        using (var canvas = new SKCanvas(source))
        {
            canvas.Clear(SKColors.Blue); using var paint = new SKPaint { Color = SKColors.Red };
            canvas.DrawRect(new SKRect(0, 0, 80, 20), paint);
        }
        using var image = SKImage.FromBitmap(source); using var encoded = image.Encode(format, 100);
        var jpeg = encoded.ToArray();
        byte[] exif = [255,225,0,34,69,120,105,102,0,0,73,73,42,0,8,0,0,0,1,0,18,1,3,0,1,0,0,0,6,0,0,0,0,0,0,0];
        using var input = new MemoryStream(format == SKEncodedImageFormat.Jpeg ? jpeg[..2].Concat(exif).Concat(jpeg[2..]).ToArray() : PngChunk(jpeg,"eXIf",exif[10..]));
        var output = await Storage().ProcessAsync(input, format == SKEncodedImageFormat.Jpeg ? ".jpg" : ".png", format == SKEncodedImageFormat.Jpeg ? "image/jpeg" : "image/png", new(0, .25, 1), default);
        using var decoded = SKBitmap.Decode(output);
        Assert.True(decoded.GetPixel(64, 128).Blue > 200);
        Assert.True(decoded.GetPixel(192, 128).Red > 200);
        Assert.False(System.Text.Encoding.Latin1.GetString(output).Contains("Exif", StringComparison.Ordinal));
    }
    private static byte[] PngChunk(byte[] png, string type, byte[] content)
    {
        var chunk = new byte[content.Length+12]; System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(0,4),(uint)content.Length);
        System.Text.Encoding.ASCII.GetBytes(type).CopyTo(chunk,4); content.CopyTo(chunk,8); uint crc=uint.MaxValue;
        for(var index=4;index<chunk.Length-4;index++) { crc ^= chunk[index]; for(var bit=0;bit<8;bit++) crc=(crc&1)!=0?(crc>>1)^0xedb88320U:crc>>1; }
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(chunk.Length-4,4),~crc);
        return png[..33].Concat(chunk).Concat(png[33..]).ToArray();
    }

    [Fact]
    public async Task CompressedPngMetadataIsNotExpandedOrRetained()
    {
        using var compressed = new MemoryStream(); compressed.Write(System.Text.Encoding.ASCII.GetBytes("Synthetic\0\0"));
        using(var zip = new System.IO.Compression.ZLibStream(compressed,System.IO.Compression.CompressionLevel.SmallestSize,true))
        { var block = new byte[8192]; Array.Fill(block,(byte)'A'); for(var index=0;index<5000;index++) zip.Write(block); }
        using var input = new MemoryStream(PngChunk(Raster(),"zTXt",compressed.ToArray()));
        var output = await Storage().ProcessAsync(input,".png","image/png",new(0,0,1),default);
        Assert.False(System.Text.Encoding.Latin1.GetString(output).Contains("zTXt",StringComparison.Ordinal));
        using var image = SKBitmap.Decode(output); Assert.Equal(256,image.Width);
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("mismatch")]
    [InlineData("truncated")]
    [InlineData("oversized")]
    [InlineData("crop")]
    public async Task UnsafeInputCannotPublish(string variant)
    {
        var bytes = variant switch { "empty" => [], "oversized" => new byte[2 * 1024 * 1024 + 1], "truncated" => Raster()[..30], _ => Raster() };
        using var input = new MemoryStream(bytes);
        await Assert.ThrowsAsync<ArgumentException>(() => Storage().ProcessAsync(input, variant == "mismatch" ? ".jpg" : ".png",
            variant == "mismatch" ? "image/jpeg" : "image/png", variant == "crop" ? new(1, 1, 1) : new(0, 0, 1), default));
    }

    [Fact]
    public async Task OversizedDecodedDimensionsAreRejectedBeforeRasterAllocation()
    {
        var bytes = Raster(); System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(16,4), 100000);
        uint crc = uint.MaxValue;
        for (var index = 12; index < 29; index++) { crc ^= bytes[index]; for(var bit=0;bit<8;bit++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xedb88320U : crc >> 1; }
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(29,4), ~crc);
        using var input = new MemoryStream(bytes);
        await Assert.ThrowsAsync<ArgumentException>(()=>Storage().ProcessAsync(input,".png","image/png",new(0,0,1),default));
    }

    /// <summary>Controlled adverse protocol replies test fail-closed parsing, not real malware detection.</summary>
    [Theory]
    [InlineData("stream: Synthetic FOUND", "Rejected")]
    [InlineData("ambiguous", "ScanUnavailable")]
    public async Task ScannerAdverseProtocolNeverGrantsClean(string reply, string expected)
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback,0); listener.Start();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            var server = Task.Run(async ()=>
            {
                using var client = await listener.AcceptTcpClientAsync(timeout.Token); using var socket = client.GetStream();
                var header = new byte[10]; await socket.ReadExactlyAsync(header,timeout.Token);
                var length = new byte[4]; await socket.ReadExactlyAsync(length,timeout.Token);
                var count = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(length); var content = new byte[count];
                await socket.ReadExactlyAsync(content,timeout.Token); await socket.ReadExactlyAsync(length,timeout.Token);
                await socket.WriteAsync(System.Text.Encoding.ASCII.GetBytes(reply+"\0"),timeout.Token);
            });
            var scanner = new ClamAvDocumentScanner(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
                ["Documents:ClamAvPort"] = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port.ToString(), ["Documents:ScanTimeoutSeconds"] = "1" }).Build());
            using var input = new MemoryStream(new byte[] {1}); Assert.Equal(expected,await scanner.ScanAsync(input,timeout.Token)); await server;
        }
        finally { listener.Stop(); }
    }

    [Fact]
    public async Task PermanentRemovalPreventsLatePublicationAndVerifiedBytesMatch()
    {
        var root = Path.Combine(Path.GetTempPath(), "hrflow-image-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root); ApiFixture.RestrictDirectory(root);
        try
        {
            var id = Guid.NewGuid(); var first = Storage(root); var independent = Storage(root);
            using var input = new MemoryStream(Raster());
            var output = await first.ProcessAsync(input, ".png", "image/png", new(0, 0, 1), default);
            var facts = await first.PublishAsync(id, output, default);
            using var verified = await independent.OpenVerifiedAsync(id, facts.Size, facts.Checksum, default);
            Assert.Equal(output, ((MemoryStream)verified).ToArray());
            var changed = output.ToArray(); changed[^1] ^= 1;
            await File.WriteAllBytesAsync(Path.Combine(root,"profile-images","clean",id.ToString("N")+".blob"),changed);
            await Assert.ThrowsAsync<IOException>(()=>independent.OpenVerifiedAsync(id,facts.Size,facts.Checksum,default));
            independent.Remove(id);
            await Assert.ThrowsAsync<IOException>(() => first.PublishAsync(id, output, default));
            Assert.False(File.Exists(Path.Combine(root, "profile-images", "clean", id.ToString("N") + ".blob")));
            Assert.True(File.Exists(Path.Combine(root, "profile-images", "removed", id.ToString("N") + ".blob")));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task SeparateProcessRemovalWinsAgainstPausedPublication()
    {
        var root = Path.Combine(Path.GetTempPath(), "hrflow-image-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root); ApiFixture.RestrictDirectory(root);
        System.Diagnostics.Process? publisher = null; System.Diagnostics.Process? remover = null;
        System.Diagnostics.Process Start(string operation, Guid id)
        {
            var start = new System.Diagnostics.ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardInput = true,
                RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            foreach (var arg in new[] { typeof(ProfileImageTests).Assembly.Location, operation, root, id.ToString() }) start.ArgumentList.Add(arg);
            var process = System.Diagnostics.Process.Start(start)!;
            _ = process.StandardError.ReadToEndAsync(); return process;
        }
        try
        {
            var id = Guid.NewGuid(); publisher = Start("--publish-image", id);
            Assert.Equal("Ready", await publisher.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10)));
            remover = Start("--remove-image", id); await remover.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10)); Assert.Equal(0, remover.ExitCode);
            await publisher.StandardInput.WriteLineAsync(); await publisher.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(0, publisher.ExitCode); Assert.Equal("Revoked", (await publisher.StandardOutput.ReadToEndAsync()).Trim());
            Assert.False(File.Exists(Path.Combine(root, "profile-images", "clean", id.ToString("N") + ".blob")));
        }
        finally
        {
            foreach (var process in new[] { publisher, remover }) if (process != null) { if (!process.HasExited) { process.Kill(true); await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5)); } process.Dispose(); }
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task RestartCompensatesInterruptedPublicationWithoutMakingItCurrent()
    {
        await using var fixture = await ApiFixture.StartAsync();
        var storage = Storage(fixture.PrivateRoot);
        var pending = new HRFlow.Domain.Entities.ProfileImage { OwnerId = fixture.Employee("employee").Id, UpdatedUtc = DateTime.UtcNow.AddHours(-2) };
        await using (var db = fixture.Context()) { db.ProfileImages.Add(pending); await db.SaveChangesAsync(); }
        using var input = new MemoryStream(Raster()); var output = await storage.ProcessAsync(input,".png","image/png",new(0,0,1),default);
        await storage.PublishAsync(pending.Id,output,default);
        await fixture.RestartAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var marker = Path.Combine(fixture.PrivateRoot,"profile-images","removed",pending.Id.ToString("N")+".blob");
        while (!File.Exists(marker)) await Task.Delay(25,timeout.Token); // Observes worker completion; does not choose a race winner.
        await using var state = fixture.Context(); Assert.Equal("Removed",(await state.ProfileImages.FindAsync(pending.Id))!.Status);
        Assert.False(File.Exists(Path.Combine(fixture.PrivateRoot,"profile-images","clean",pending.Id.ToString("N")+".blob")));
        await Assert.ThrowsAsync<IOException>(()=>storage.PublishAsync(pending.Id,output,default));
        var token = await fixture.LoginAsync("employee"); Assert.Equal(System.Net.HttpStatusCode.NotFound,(await fixture.SendAsync(token,"me/image/content")).StatusCode);
    }

    [Fact]
    public async Task ScannerOutageLeavesNoCurrentReplacement()
    {
        // This real HTTP fixture has no scanner dependency. Failure is exercised, never treated as a clean verdict.
        await using var fixture = await ApiFixture.StartAsync(); var token = await fixture.LoginAsync("employee");
        Assert.Equal(System.Net.HttpStatusCode.OK, (await fixture.SendAsync(token, "me/image")).StatusCode);
        using var upload = await fixture.UploadImageAsync(token, Raster(), Guid.Empty);
        Assert.Equal(System.Net.HttpStatusCode.Conflict, upload.StatusCode);
        await using var db = fixture.Context(); var rows = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(db.ProfileImages);
        Assert.Single(rows); Assert.All(rows, row => Assert.Equal("Removed", row.Status));
        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await fixture.SendAsync(token, $"employees/{fixture.Employee("manager").Id}/image")).StatusCode);
    }

    /// <summary>Default is real fail-closed transport; explicit ClamAV enables the same retained HTTP/row/concurrency path.</summary>
    [Fact]
    public async Task ImageHttpHonoursExplicitScannerConfiguration()
    {
        var realScanner = int.TryParse(Environment.GetEnvironmentVariable("HRFLOW_TEST_CLAMAV_PORT"), out var port);
        await using var fixture = await ApiFixture.StartAsync(scannerPort: realScanner ? port : 0);
        var owner = await fixture.LoginAsync("employee"); var manager = await fixture.LoginAsync("manager"); var hr = await fixture.LoginAsync("hr");
        using var first = await fixture.UploadImageAsync(owner, Raster(), Guid.Empty);
        if (!realScanner)
        {
            Assert.Equal(System.Net.HttpStatusCode.Conflict, first.StatusCode);
            await using var closed = fixture.Context(); Assert.False(await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(closed.ProfileImages, i => i.Status == "Current")); return;
        }
        Assert.Equal(System.Net.HttpStatusCode.OK, first.StatusCode);
        var version = (await ApiFixture.JsonAsync(first))["version"]!.GetValue<Guid>();
        var employee = fixture.Employee("employee").Id;
        using (var connection = fixture.Connection())
        {
            using var trigger = connection.CreateCommand();
            trigger.CommandText = "CREATE TRIGGER FailImageSwitch BEFORE UPDATE OF Status ON ProfileImages WHEN NEW.Status='Current' BEGIN SELECT RAISE(ABORT,'synthetic late failure'); END"; trigger.ExecuteNonQuery();
            using var failed = await fixture.UploadImageAsync(owner, Raster(), version);
            Assert.Equal(System.Net.HttpStatusCode.InternalServerError, failed.StatusCode);
            await using var state = fixture.Context();
            var old = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync(state.ProfileImages, i => i.OwnerId == employee && i.Status == "Current");
            Assert.Equal(version, old.Version);
            var compensated = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync(state.ProfileImages, i => i.OwnerId == employee && i.Status == "Removed");
            Assert.False(File.Exists(Path.Combine(fixture.PrivateRoot,"profile-images","clean",compensated.Id.ToString("N")+".blob")));
            Assert.True(File.Exists(Path.Combine(fixture.PrivateRoot,"profile-images","removed",compensated.Id.ToString("N")+".blob")));
            trigger.CommandText = "DROP TRIGGER FailImageSwitch"; trigger.ExecuteNonQuery();
        }
        Assert.Equal(System.Net.HttpStatusCode.OK, (await fixture.SendAsync(hr, $"employees/{employee}/image")).StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await fixture.SendAsync(manager, $"employees/{employee}/image")).StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.MethodNotAllowed, (await fixture.SendAsync(hr, $"employees/{employee}/image", System.Net.Http.HttpMethod.Delete)).StatusCode);
        using (var connection = fixture.Connection())
        {
            using var writer = connection.BeginTransaction(deferred: false);
            var entered = fixture.EnteredAsync("ProfileImagesController.Replace", 2);
            var a = fixture.UploadImageAsync(owner, Raster(), version); var b = fixture.UploadImageAsync(owner, Raster(), version, 1);
            await entered; writer.Commit();
            var results = await Task.WhenAll(a, b);
            Assert.Single(results, r => r.StatusCode == System.Net.HttpStatusCode.OK);
            Assert.Single(results, r => r.StatusCode == System.Net.HttpStatusCode.Conflict);
            foreach (var result in results) result.Dispose();
        }
        await using var db = fixture.Context();
        var current = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync(db.ProfileImages, i => i.OwnerId == employee && i.Status == "Current");
        Assert.NotEqual(version, current.Version); Assert.True(current.Size > 0);
        using (var connection = fixture.Connection())
        {
            using var writer = connection.BeginTransaction(deferred: false);
            var replacing = fixture.EnteredAsync("ProfileImagesController.Replace"); var removing = fixture.EnteredAsync("ProfileImagesController.Remove");
            var replacement = fixture.UploadImageAsync(owner, Raster(), current.Version);
            var removal = fixture.SendAsync(owner, $"me/image?expectedVersion={current.Version}", System.Net.Http.HttpMethod.Delete, server: 1);
            await Task.WhenAll(replacing,removing); writer.Commit();
            using var replaced = await replacement; using var deleted = await removal;
            Assert.True(replaced.StatusCode == System.Net.HttpStatusCode.OK && deleted.StatusCode == System.Net.HttpStatusCode.Conflict
                || replaced.StatusCode == System.Net.HttpStatusCode.Conflict && deleted.StatusCode == System.Net.HttpStatusCode.NoContent);
            db.ChangeTracker.Clear(); current = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync(db.ProfileImages, i => i.OwnerId == employee && i.Status == "Current");
            Assert.Equal(deleted.StatusCode == System.Net.HttpStatusCode.NoContent, current.Size == 0);
        }
        using var stale = await fixture.SendAsync(owner, $"me/image?expectedVersion={version}", System.Net.Http.HttpMethod.Delete);
        Assert.Equal(System.Net.HttpStatusCode.Conflict, stale.StatusCode);
        using var removed = await fixture.SendAsync(owner, $"me/image?expectedVersion={current.Version}", System.Net.Http.HttpMethod.Delete);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, removed.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await fixture.SendAsync(owner, "me/image/content")).StatusCode);
        db.ChangeTracker.Clear(); var empty = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync(db.ProfileImages, i => i.OwnerId == employee && i.Status == "Current");
        Assert.Equal(0, empty.Size); Assert.NotEqual(current.Version, empty.Version);
        Assert.False(await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.AuditEntries));
    }
}
