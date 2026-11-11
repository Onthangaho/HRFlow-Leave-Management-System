using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using HRFlow.Api.Middleware;
using Xunit;

namespace HRFlow.RegressionTests;

/// <summary>Classifies only confirmed client aborts at the HTTP boundary; unrelated failures remain observable.</summary>
public sealed class RequestCancellationLoggingTests
{
    /// <summary>Cancellation before MVC and a controller's completed 499 have the same informational outcome.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ConfirmedClientAbortIsInformational(bool throws)
    {
        using var abort = new CancellationTokenSource();
        var context = Context(); context.RequestAborted = abort.Token;
        var logger = new RecordingLogger();
        var middleware = new ApiRequestLoggingMiddleware(_ =>
        {
            abort.Cancel();
            if (throws) return Task.FromException(new OperationCanceledException(abort.Token));
            context.Response.StatusCode = 499; return Task.CompletedTask;
        }, logger);
        await middleware.InvokeAsync(context);
        Assert.Equal(499, context.Response.StatusCode);
        Assert.Equal(new[] { LogLevel.Information }, logger.Levels);
        Assert.Equal("safe-regression", context.Response.Headers["X-Correlation-ID"].ToString());
    }

    /// <summary>An unrelated cancellation must not be mistaken for navigation or silently converted to 499.</summary>
    [Fact]
    public async Task UnexpectedCancellationPropagates()
    {
        var logger = new RecordingLogger();
        var middleware = new ApiRequestLoggingMiddleware(_ => Task.FromException(new OperationCanceledException()), logger);
        await Assert.ThrowsAsync<OperationCanceledException>(() => middleware.InvokeAsync(Context()));
        Assert.Empty(logger.Levels);
    }

    /// <summary>Server failures and unexplained 499s retain their existing error/warning classification.</summary>
    [Theory]
    [InlineData(500, LogLevel.Error)]
    [InlineData(499, LogLevel.Warning)]
    public async Task OtherOutcomesRemainVisible(int status, LogLevel level)
    {
        var logger = new RecordingLogger();
        var middleware = new ApiRequestLoggingMiddleware(context => { context.Response.StatusCode = status; return Task.CompletedTask; }, logger);
        await middleware.InvokeAsync(Context());
        Assert.Equal(new[] { level }, logger.Levels);
    }

    private static DefaultHttpContext Context()
    {
        var context = new DefaultHttpContext(); context.Request.Path = "/api/v1/employees";
        context.Request.Headers["X-Correlation-ID"] = "safe-regression";
        return context;
    }

    private sealed class RecordingLogger : ILogger<ApiRequestLoggingMiddleware>
    {
        public List<LogLevel> Levels { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Levels.Add(logLevel);
    }
}
