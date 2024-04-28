using Microsoft.Extensions.Logging;

namespace AeFinder.Sdk.Logging;

public static class AeFinderLoggerExtensions
{
    public static void LogDebug(this IAeFinderLogger logger, Exception? exception, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Debug, exception, message, args);
    }
    
    public static void LogDebug(this IAeFinderLogger logger, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Debug, message, args);
    }
    
    public static void LogTrace(this IAeFinderLogger logger, Exception? exception, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Trace, exception, message, args);
    }
    
    public static void LogTrace(this IAeFinderLogger logger, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Trace, message, args);
    }
 
    public static void LogInformation(this IAeFinderLogger logger, Exception? exception, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Information, exception, message, args);
    }
    
    public static void LogInformation(this IAeFinderLogger logger, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Information, message, args);
    }
    
    public static void LogWarning(this IAeFinderLogger logger, Exception? exception, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Warning, exception, message, args);
    }
    
    public static void LogWarning(this IAeFinderLogger logger, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Warning, message, args);
    }
    
    public static void LogError(this IAeFinderLogger logger, Exception? exception, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Error, exception, message, args);
    }
    
    public static void LogError(this IAeFinderLogger logger, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Error, message, args);
    }
    
    public static void LogCritical(this IAeFinderLogger logger, Exception? exception, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Critical, exception, message, args);
    }
    
    public static void LogCritical(this IAeFinderLogger logger, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Critical, message, args);
    }
    
    public static void Log(this IAeFinderLogger logger,LogLevel logLevel, string? message, params object?[] args)
    {
        logger.Log(logLevel, null, message, args);
    }
}