using Microsoft.Extensions.Logging;

namespace AElfIndexer.Sdk.Logging;

public interface IIndexerLogger
{
    void Log(LogLevel logLevel, Exception? exception, string? message, params object?[] args);
}