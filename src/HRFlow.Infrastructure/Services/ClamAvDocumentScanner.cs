using HRFlow.Domain.Entities;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using HRFlow.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace HRFlow.Infrastructure.Services;

/// <summary>Real local clamd INSTREAM protocol; only an exact affirmative verdict grants clean status.</summary>
public sealed class ClamAvDocumentScanner(IConfiguration configuration) : IDocumentScanner
{
    /// <inheritdoc />
    public async Task<string> ScanAsync(Stream input, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        var seconds = configuration.GetValue<int?>("Documents:ScanTimeoutSeconds") ?? 30;
        if (seconds is < 1 or > 60) throw new InvalidOperationException("Scanner timeout must be within 1–60 seconds.");
        timeout.CancelAfter(TimeSpan.FromSeconds(seconds)); var ct = timeout.Token;
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, configuration.GetValue<int?>("Documents:ClamAvPort") ?? 3310, ct);
            using var socket = client.GetStream(); await socket.WriteAsync("zINSTREAM\0"u8.ToArray(), ct);
            var buffer = new byte[81920]; var length = new byte[4]; int count;
            while ((count = await input.ReadAsync(buffer, ct)) != 0)
            { BinaryPrimitives.WriteUInt32BigEndian(length, (uint)count); await socket.WriteAsync(length, ct); await socket.WriteAsync(buffer.AsMemory(0, count), ct); }
            await socket.WriteAsync(new byte[4], ct);
            using var response = new MemoryStream(); var one = new byte[1];
            while (response.Length < 1024 && await socket.ReadAsync(one, ct) == 1)
            { if (one[0] == 0) { var verdict = Encoding.ASCII.GetString(response.ToArray()); return verdict == "stream: OK" ? SupportingDocumentStatus.Clean : verdict.StartsWith("stream: ", StringComparison.Ordinal) && verdict.EndsWith(" FOUND", StringComparison.Ordinal) ? SupportingDocumentStatus.Rejected : SupportingDocumentStatus.ScanUnavailable; } response.WriteByte(one[0]); }
            return SupportingDocumentStatus.ScanUnavailable;
        }
        catch (Exception exception) when (exception is IOException or SocketException or OperationCanceledException) { return SupportingDocumentStatus.ScanUnavailable; }
    }
}
