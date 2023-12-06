using Microsoft.Extensions.Logging;

namespace AElfIndexer.Sdk.Logging;

public static class IndexerLoggerExtensions
{
    public static void LogDebug(this IIndexerLogger logger, Exception? exception, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Debug, exception, message, args);
    }
    
    public static void LogDebug(this IIndexerLogger logger, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Debug, message, args);
    }
    
    public static void LogTrace(this IIndexerLogger logger, Exception? exception, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Trace, exception, message, args);
    }
    
    public static void LogTrace(this IIndexerLogger logger, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Trace, message, args);
    }
 
    public static void LogInformation(this IIndexerLogger logger, Exception? exception, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Information, exception, message, args);
    }
    
    public static void LogInformation(this IIndexerLogger logger, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Information, message, args);
    }
    
    public static void LogWarning(this IIndexerLogger logger, Exception? exception, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Warning, exception, message, args);
    }
    
    public static void LogWarning(this IIndexerLogger logger, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Warning, message, args);
    }
    
    public static void LogError(this IIndexerLogger logger, Exception? exception, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Error, exception, message, args);
    }
    
    public static void LogError(this IIndexerLogger logger, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Error, message, args);
    }
    
    public static void LogCritical(this IIndexerLogger logger, Exception? exception, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Critical, exception, message, args);
    }
    
    public static void LogCritical(this IIndexerLogger logger, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Critical, message, args);
    }
    
    public static void Log(this IIndexerLogger logger,LogLevel logLevel, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Critical, null, message, args);
    }
}