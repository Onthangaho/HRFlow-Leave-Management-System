using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using HRFlow.Infrastructure.Services.Auth;

namespace HRFlow.RegressionTests;

/// <summary>Test assembly only: real MIME over a fixed loopback endpoint; there is no forwarding or provider configuration.</summary>
internal sealed class PrivateActivationSmtp : IActivationDelivery, IAsyncDisposable
{
    private readonly TcpListener listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource stopped = new();
    private readonly ConcurrentBag<Task> connections = [];
    private readonly Channel<string> messages = Channel.CreateUnbounded<string>();
    private readonly Task accepting;
    private int submissions;
    private int attempts;
    private readonly ConcurrentDictionary<string, byte> accepted = new();
    public string Mode { get; set; } = "accept";
    public Func<Task>? AfterMessage { get; set; }
    public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int Submissions => submissions;
    public int Attempts => attempts;
    public PrivateActivationSmtp() { listener.Start(); accepting = AcceptAsync(); }
    public void EnsureConfigured() { if (stopped.IsCancellationRequested) throw new InvalidOperationException("Private fixture stopped."); }
    public async Task<ActivationDeliveryOutcome> DeliverAsync(Guid accountId, string email, Uri link, CancellationToken token)
    {
        EnsureConfigured();
        Interlocked.Increment(ref attempts);
        if (!email.EndsWith("@synthetic.invalid", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Synthetic recipients required.");
        using var message = new MailMessage("activation@synthetic.invalid", email) { Subject = "Activate synthetic HRFlow account", BodyEncoding = Encoding.UTF8 };
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString("Establish your password: " + link.AbsoluteUri, Encoding.UTF8, MediaTypeNames.Text.Plain));
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString("<p>Establish your password: <a href=\"" + WebUtility.HtmlEncode(link.AbsoluteUri) + "\">Activate account</a></p>", Encoding.UTF8, MediaTypeNames.Text.Html));
        using var client = new SmtpClient(IPAddress.Loopback.ToString(), ((IPEndPoint)listener.LocalEndpoint).Port)
            { DeliveryMethod = SmtpDeliveryMethod.Network, UseDefaultCredentials = false, EnableSsl = false };
        try {
            await client.SendMailAsync(message, token);
            // SmtpClient can complete on an EOF after DATA. This private fixture additionally requires
            // its sink's positive acceptance marker; it is not a production SMTP implementation.
            return accepted.ContainsKey(link.Fragment) ? ActivationDeliveryOutcome.Accepted : ActivationDeliveryOutcome.Unknown;
        }
        catch (SmtpFailedRecipientException) { return ActivationDeliveryOutcome.Failed; }
        catch (SmtpException error) when ((int)error.StatusCode >= 400) { return ActivationDeliveryOutcome.Failed; }
        catch (Exception error) when (error is SmtpException or IOException or OperationCanceledException) { return ActivationDeliveryOutcome.Unknown; }
    }
    public Task<string> MessageAsync() => messages.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
    public static string Invitation(string mime)
    {
        // This bounded parser is a test assertion helper, not a production MIME implementation.
        var decoded = new StringBuilder(mime);
        foreach (var part in mime.Split("Content-Transfer-Encoding: base64", StringSplitOptions.None).Skip(1))
        {
            var start = part.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (start < 0) continue;
            var body = part[(start + 4)..].Split("\r\n--", StringSplitOptions.None)[0];
            decoded.Append(Encoding.UTF8.GetString(Convert.FromBase64String(body.Trim())));
        }
        var match = Regex.Match(decoded.ToString(), "#token=([A-F0-9]{64})");
        if (!match.Success) throw new InvalidOperationException("Synthetic message has no invitation.");
        return match.Groups[1].Value;
    }
    private async Task AcceptAsync()
    {
        try { while (!stopped.IsCancellationRequested) connections.Add(HandleAsync(await listener.AcceptTcpClientAsync(stopped.Token))); }
        catch (Exception e) when (e is OperationCanceledException or SocketException or ObjectDisposedException) { }
    }
    private async Task HandleAsync(TcpClient socket)
    {
        using (socket)
        using (var lifetime = CancellationTokenSource.CreateLinkedTokenSource(stopped.Token))
        {
            lifetime.CancelAfter(TimeSpan.FromSeconds(20));
            var token = lifetime.Token;
            using var reader = new StreamReader(socket.GetStream(), Encoding.ASCII);
            using var writer = new StreamWriter(socket.GetStream(), Encoding.ASCII) { AutoFlush = true, NewLine = "\r\n" };
            try
            {
                await writer.WriteLineAsync("220 private synthetic fixture");
                while (await reader.ReadLineAsync(token) is {} line)
                {
                    if (line.Length > 4096) return;
                    if (line.StartsWith("EHLO ", StringComparison.Ordinal) || line.StartsWith("HELO ", StringComparison.Ordinal)) await writer.WriteLineAsync("250 private-loopback");
                    else if (line.StartsWith("MAIL FROM:", StringComparison.Ordinal)) await writer.WriteLineAsync("250 OK");
                    else if (line.StartsWith("RCPT TO:", StringComparison.Ordinal)) await writer.WriteLineAsync(Mode == "reject" || !line.EndsWith("@synthetic.invalid>", StringComparison.OrdinalIgnoreCase) ? "550 synthetic rejection" : "250 OK");
                    else if (line == "DATA")
                    {
                        await writer.WriteLineAsync("354 End with dot");
                        var data = new StringBuilder();
                        while (await reader.ReadLineAsync(token) is {} body && body != ".")
                        { if (body.Length > 4096 || data.Length + body.Length > 16384) return; data.Append(body).Append("\r\n"); }
                        Interlocked.Increment(ref submissions); messages.Writer.TryWrite(data.ToString()); Entered.TrySetResult();
                        if (AfterMessage is not null) await AfterMessage();
                        if (Mode == "hold") await Release.Task.WaitAsync(token);
                        if (Mode == "timeout") await Task.Delay(Timeout.Infinite, token);
                        if (Mode == "disconnect") return;
                        accepted.TryAdd("#token=" + Invitation(data.ToString()), 0);
                        await writer.WriteLineAsync("250 Accepted for synthetic sink");
                    }
                    else if (line == "QUIT") { await writer.WriteLineAsync("221 Bye"); return; }
                    else if (line == "RSET") await writer.WriteLineAsync("250 OK");
                    else await writer.WriteLineAsync("500 Unsupported");
                }
            }
            catch (Exception e) when (e is IOException or OperationCanceledException or ObjectDisposedException) { }
        }
    }
    public async ValueTask DisposeAsync()
    {
        Release.TrySetResult(); stopped.Cancel(); listener.Stop(); await accepting;
        await Task.WhenAll(connections).WaitAsync(TimeSpan.FromSeconds(5)); accepted.Clear(); messages.Writer.TryComplete(); while (messages.Reader.TryRead(out _)) { } stopped.Dispose();
    }
}
