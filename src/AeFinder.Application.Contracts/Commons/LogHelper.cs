using Serilog;
using Serilog.Events;

namespace AeFinder.Commons;

public static class LogHelper
{
    public static ILogger CreateLogger(LogEventLevel logEventLevel)
    {
        return new LoggerConfiguration()
            .MinimumLevel.Is(logEventLevel)
            .Enrich.FromLogContext()
            .WriteTo.Async(c =>
                c.Console(
                    outputTemplate:
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}{Offset:zzz}][{Level:u3}] [{SourceContext}] {Message}{NewLine}{Exception}"))
            .WriteTo.Async(c => c.File("Logs/log-.log", rollingInterval: RollingInterval.Day,
                outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}{Offset:zzz}][{Level:u3}] [{SourceContext}] {Message}{NewLine}{Exception}"))
            .CreateLogger();
    }
}