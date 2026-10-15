using System.Security.Cryptography;
using HRFlow.Application.Interfaces;
using HRFlow.Infrastructure.Services.Operations;
using Microsoft.Extensions.Configuration;
using SkiaSharp;

namespace HRFlow.Infrastructure.Services;

/// <summary>Bounded raster processing and tombstoned cross-process publication in an image-only namespace.</summary>
public sealed class PrivateProfileImageStorage(IConfiguration configuration) : IProfileImageStorage
{
    public const int MaxInputBytes = 2 * 1024 * 1024;
    public const int MaxPixels = 4_000_000;
    public const int MaxDimension = 4096;
    public const int OutputDimension = 256;
    private readonly string root = configuration["Storage:PrivateFilesRoot"] ?? "";

    /// <inheritdoc />
    public async Task<byte[]> ProcessAsync(Stream input, string extension, string mime, ImageCrop crop, CancellationToken token)
    {
        if (!double.IsFinite(crop.X) || !double.IsFinite(crop.Y) || !double.IsFinite(crop.Size)
            || crop.X < 0 || crop.Y < 0 || crop.Size <= 0 || crop.Size > 1)
            throw new ArgumentException("Choose a valid square crop.");
        using var memory = new MemoryStream();
        var buffer = new byte[81920]; int count;
        while ((count = await input.ReadAsync(buffer, token)) != 0)
        {
            if (memory.Length + count > MaxInputBytes) throw new ArgumentException("Images must be at most 2 MiB.");
            await memory.WriteAsync(buffer.AsMemory(0, count), token);
        }
        var bytes = memory.ToArray();
        byte[]? normalizedPng = null;
        int? pngOrigin = null;
        try
        {
            var png = extension.Equals(".png", StringComparison.OrdinalIgnoreCase) && mime == "image/png"
                && bytes.AsSpan().StartsWith(new byte[] {137,80,78,71,13,10,26,10});
            var jpeg = (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
                && mime == "image/jpeg" && bytes.Length >= 4 && bytes[0] == 255 && bytes[1] == 216 && bytes[^2] == 255 && bytes[^1] == 217;
            if (!png && !jpeg) throw new ArgumentException("Choose a complete JPEG or PNG matching its file type.");
            if (png) { ValidatePng(bytes); pngOrigin = PngOrientation(bytes); normalizedPng = PngWithoutOptionalMetadata(bytes); }
            using var data = SKData.CreateCopy(normalizedPng ?? bytes);
            using var codec = SKCodec.Create(data);
            if (codec == null || codec.Info.Width <= 0 || codec.Info.Height <= 0 || codec.Info.Width > MaxDimension
                || codec.Info.Height > MaxDimension || (long)codec.Info.Width * codec.Info.Height > MaxPixels || codec.FrameCount > 1
                || codec.EncodedFormat != (png ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Jpeg))
                throw new ArgumentException("Use a single-frame image up to 4096 pixels per side and 4 megapixels.");
            // Two bounded 32-bit raster buffers require at most 32 MiB; no source-sized allocation precedes these checks.
            var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var decoded = new SKBitmap(info);
            if (codec.GetPixels(info, decoded.GetPixels()) != SKCodecResult.Success) throw new ArgumentException("Image is truncated or malformed.");
            var origin = pngOrigin ?? (int)codec.EncodedOrigin;
            var swap = origin >= 5;
            using var oriented = new SKBitmap(swap ? info.Height : info.Width, swap ? info.Width : info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            // Apply all eight EXIF orientations explicitly; metadata is never copied into the new output.
            for (var y = 0; y < info.Height; y++) for (var x = 0; x < info.Width; x++)
            {
                var (dx, dy) = origin switch
                {
                    2 => (info.Width - 1 - x, y), 3 => (info.Width - 1 - x, info.Height - 1 - y),
                    4 => (x, info.Height - 1 - y), 5 => (y, x), 6 => (info.Height - 1 - y, x),
                    7 => (info.Height - 1 - y, info.Width - 1 - x), 8 => (y, info.Width - 1 - x), _ => (x, y)
                };
                oriented.SetPixel(dx, dy, decoded.GetPixel(x, y));
            }
            var side = Math.Min(oriented.Width, oriented.Height) * crop.Size;
            var left = crop.X * oriented.Width; var top = crop.Y * oriented.Height;
            if (side < 1 || left + side > oriented.Width + .001 || top + side > oriented.Height + .001)
                throw new ArgumentException("Square crop must fit inside the oriented image.");
            using var output = new SKBitmap(OutputDimension, OutputDimension, SKColorType.Rgba8888, SKAlphaType.Premul);
            using (var canvas = new SKCanvas(output))
                canvas.DrawBitmap(oriented, new SKRect((float)left, (float)top, (float)(left + side), (float)(top + side)), new SKRect(0, 0, OutputDimension, OutputDimension), new SKSamplingOptions(SKFilterMode.Linear));
            using var image = SKImage.FromBitmap(output);
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            return encoded.ToArray();
        }
        finally { CryptographicOperations.ZeroMemory(bytes); if (normalizedPng != null) CryptographicOperations.ZeroMemory(normalizedPng); }
    }
    private static int PngOrientation(byte[] bytes)
    {
        int? result = null;
        for (var offset = 8; offset < bytes.Length;)
        {
            var size = (int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset,4));
            if (bytes.AsSpan(offset+4,4).SequenceEqual("eXIf"u8))
            {
                if (result != null || size > 65536) throw new ArgumentException("Invalid PNG orientation metadata.");
                result = TiffOrientation(bytes.AsSpan(offset+8,size));
            }
            offset += size+12;
        }
        return result ?? 1;
    }
    private static int TiffOrientation(ReadOnlySpan<byte> bytes)
    {
        // SKCodec exposes JPEG origin but not PNG eXIf in the installed release. Only bounded IFD0
        // orientation is read here; no thumbnails, GPS pointers or compressed metadata are traversed.
        if (bytes.Length < 8) throw new ArgumentException("Invalid PNG orientation metadata.");
        var little = bytes[..2].SequenceEqual("II"u8);
        if (!little && !bytes[..2].SequenceEqual("MM"u8)) throw new ArgumentException("Invalid PNG orientation metadata.");
        static ushort U16(ReadOnlySpan<byte> value, bool little) => little ? System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(value) : System.Buffers.Binary.BinaryPrimitives.ReadUInt16BigEndian(value);
        static uint U32(ReadOnlySpan<byte> value, bool little) => little ? System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(value) : System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(value);
        var start = U32(bytes.Slice(4,4),little);
        if (U16(bytes.Slice(2,2),little) != 42 || start < 8 || start > bytes.Length-2) throw new ArgumentException("Invalid PNG orientation metadata.");
        var count = U16(bytes.Slice((int)start,2),little);
        if (count > 4096 || start+2L+count*12L > bytes.Length) throw new ArgumentException("Invalid PNG orientation metadata.");
        int? origin = null;
        for(var index=0;index<count;index++)
        {
            var entry = bytes.Slice((int)start+2+index*12,12);
            if (U16(entry[..2],little) != 0x0112) continue;
            if (origin != null || U16(entry.Slice(2,2),little) != 3 || U32(entry.Slice(4,4),little) != 1) throw new ArgumentException("Invalid PNG orientation metadata.");
            origin = U16(entry.Slice(8,2),little);
            if (origin is < 1 or > 8) throw new ArgumentException("Invalid PNG orientation metadata.");
        }
        return origin ?? 1;
    }
    private static byte[] PngWithoutOptionalMetadata(byte[] bytes)
    {
        // Do not let compressed text/ICC chunks allocate unbounded metadata before raster dimension checks.
        using var result = new MemoryStream(); result.Write(bytes, 0, 8);
        for (var offset = 8; offset < bytes.Length;)
        {
            var size = (int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset,4));
            var type = System.Text.Encoding.ASCII.GetString(bytes,offset+4,4);
            if (type is "IHDR" or "PLTE" or "IDAT" or "IEND" or "tRNS")
            {
                result.Write(bytes,offset,size+12);
            }
            else if ((bytes[offset+4] & 32) == 0) throw new ArgumentException("Unsupported critical PNG content.");
            offset += size + 12;
        }
        return result.ToArray();
    }
    private static void ValidatePng(byte[] bytes)
    {
        var offset = 8; var ended = false;
        while (offset < bytes.Length)
        {
            if (bytes.Length - offset < 12) throw new ArgumentException("Malformed PNG.");
            var length = System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4));
            if (length > bytes.Length - offset - 12) throw new ArgumentException("Malformed PNG.");
            var size = (int)length;
            var type = bytes.AsSpan(offset + 4, 4);
            if (offset == 8 && (!type.SequenceEqual("IHDR"u8) || size != 13) || type.SequenceEqual("acTL"u8))
                throw new ArgumentException("Use a complete, non-animated PNG.");
            uint crc = uint.MaxValue;
            foreach (var value in bytes.AsSpan(offset + 4, size + 4))
            {
                crc ^= value;
                for (var bit = 0; bit < 8; bit++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xedb88320U : crc >> 1;
            }
            if (~crc != System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset + 8 + size, 4)))
                throw new ArgumentException("PNG checksum failed.");
            if (type.SequenceEqual("IEND"u8)) { if (size != 0 || offset + 12 != bytes.Length) throw new ArgumentException("Malformed PNG ending."); ended = true; }
            offset += size + 12;
        }
        if (!ended) throw new ArgumentException("Incomplete PNG.");
    }

    /// <inheritdoc />
    public async Task<StoredDocument> PublishAsync(Guid id, byte[] output, CancellationToken token)
    {
        if (output.Length is < 1 or > 512 * 1024) throw new IOException("Invalid processed image size.");
        using var gate = Lock(id);
        if (File.Exists(PathFor(id, "removed"))) throw new IOException("Image publication was revoked.");
        var staging = PathFor(id, "staging");
        try
        {
            await using (var file = RestrictedFile(staging, FileMode.CreateNew, FileAccess.Write))
            { await file.WriteAsync(output, token); file.Flush(true); }
            File.Move(staging, PathFor(id, "clean"), false);
            return new(output.Length, Convert.ToHexString(SHA256.HashData(output)), "image/png");
        }
        finally { if (File.Exists(staging)) File.Delete(staging); }
    }

    /// <inheritdoc />
    public async Task<Stream> OpenVerifiedAsync(Guid id, long size, string checksum, CancellationToken token)
    {
        if (size is < 1 or > 512 * 1024) throw new IOException("Image integrity check failed.");
        using var file = File.OpenRead(PathFor(id, "clean"));
        if (file.Length != size) throw new IOException("Image integrity check failed.");
        var bytes = new byte[(int)size]; await file.ReadExactlyAsync(bytes, token);
        if (Convert.ToHexString(SHA256.HashData(bytes)) != checksum) throw new IOException("Image integrity check failed.");
        return new MemoryStream(bytes, false);
    }

    /// <inheritdoc />
    public void Remove(Guid id)
    {
        using var gate = Lock(id);
        using (var marker = RestrictedFile(PathFor(id, "removed"), FileMode.OpenOrCreate, FileAccess.Write)) marker.Flush(true);
        foreach (var area in new[] { "staging", "clean" }) { var path = PathFor(id, area); if (File.Exists(path)) File.Delete(path); }
    }
    private FileStream Lock(Guid id)
    {
        var started = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            try { return RestrictedFile(PathFor(id, "locks"), FileMode.OpenOrCreate, FileAccess.ReadWrite); }
            catch (IOException) when (started.Elapsed < TimeSpan.FromSeconds(3)) { Thread.Sleep(25); }
        }
    }
    private static FileStream RestrictedFile(string path, FileMode mode, FileAccess access)
    {
        var options = new FileStreamOptions { Mode = mode, Access = access, Share = FileShare.None };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        return new FileStream(path, options);
    }
    private string PathFor(Guid id, string area)
    {
        if (!Path.IsPathFullyQualified(root) || !Directory.Exists(root)) throw new IOException("Provision private image storage first.");
        PrivateOperationsPaths.Check(root, Directory.GetCurrentDirectory());
        var directory = Path.Combine(root, "profile-images", area);
        for (var ancestor = new DirectoryInfo(directory); ancestor != null; ancestor = ancestor.Parent)
            if (ancestor.Exists && ancestor.Attributes.HasFlag(FileAttributes.ReparsePoint)) throw new IOException("Unsafe image storage.");
        foreach (var ownedDirectory in new[] { Path.Combine(root, "profile-images"), directory })
        {
            if (OperatingSystem.IsWindows()) Directory.CreateDirectory(ownedDirectory);
            else Directory.CreateDirectory(ownedDirectory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(ownedDirectory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
        var path = Path.Combine(directory, id.ToString("N") + ".blob");
        if (File.Exists(path) && File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint)) throw new IOException("Unsafe image object.");
        return path;
    }
}
