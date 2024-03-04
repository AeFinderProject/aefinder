using AElfIndexer.App.OperationLimits;
using AElfIndexer.Sdk.Logging;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.App.Logging;

public class IndexerLogger : IIndexerLogger, ISingletonDependency
{
    private readonly ILogger<IndexerLogger> _logger;
    private readonly ILogOperationLimitProvider _logOperationLimitProvider;

    public IndexerLogger(ILogger<IndexerLogger> logger, ILogOperationLimitProvider logOperationLimitProvider)
    {
        _logger = logger;
        _logOperationLimitProvider = logOperationLimitProvider;
    }

    public void Log(LogLevel logLevel, Exception exception, string message, params object[] args)
    {
        _logOperationLimitProvider.CheckLog(exception, message, args);
        _logger.Log(logLevel,AElfIndexerApplicationConsts.AppLogEventId, exception, message, args);
    }
}