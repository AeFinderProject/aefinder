using Microsoft.Extensions.Logging;

namespace AeFinder.Sdk.Logging;

public interface IAeFinderLogger
{
    void Log(LogLevel logLevel, Exception? exception, string? message, params object?[] args);
}