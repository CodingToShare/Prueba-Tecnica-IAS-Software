using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace ServiceRequests.Tests.TestSupport;

/// <summary>A captured log entry: level, category, event and formatted message.</summary>
internal sealed record LogEntry(LogLevel Level, string Category, EventId EventId, string Message);

/// <summary>Collects formatted log messages so tests can assert on observable behavior (e.g. retries).</summary>
internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();

    public IReadOnlyList<LogEntry> Entries => _entries.ToArray();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, _entries);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(string category, ConcurrentQueue<LogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            entries.Enqueue(new LogEntry(logLevel, category, eventId, formatter(state, exception)));
    }
}
