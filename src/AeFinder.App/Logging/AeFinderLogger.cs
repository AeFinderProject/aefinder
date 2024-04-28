using AeFinder.App.OperationLimits;
using AeFinder.Sdk.Logging;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AeFinder.App.Logging;

public class AeFinderLogger : IAeFinderLogger, ISingletonDependency
{
    private readonly ILogger<AeFinderLogger> _logger;
    private readonly ILogOperationLimitProvider _logOperationLimitProvider;

    public AeFinderLogger(ILogger<AeFinderLogger> logger, ILogOperationLimitProvider logOperationLimitProvider)
    {
        _logger = logger;
        _logOperationLimitProvider = logOperationLimitProvider;
    }

    public void Log(LogLevel logLevel, Exception exception, string message, params object[] args)
    {
        _logOperationLimitProvider.CheckLog(exception, message, args);
        _logger.Log(logLevel,AeFinderApplicationConsts.AppLogEventId, exception, message, args);
    }
}