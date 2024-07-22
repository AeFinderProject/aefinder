using AeFinder.App.BlockProcessing;
using AeFinder.App.OperationLimits;
using AeFinder.Sdk.Logging;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AeFinder.App.Logging;

public class AeFinderLogger : IAeFinderLogger, ISingletonDependency
{
    private readonly ILogger<AeFinderLogger> _logger;
    private readonly ILogOperationLimitProvider _logOperationLimitProvider;
    private readonly IBlockProcessingContext _blockProcessingContext;

    public AeFinderLogger(ILogger<AeFinderLogger> logger, ILogOperationLimitProvider logOperationLimitProvider,
        IBlockProcessingContext blockProcessingContext)
    {
        _logger = logger;
        _logOperationLimitProvider = logOperationLimitProvider;
        _blockProcessingContext = blockProcessingContext;
    }

    public void Log(LogLevel logLevel, Exception exception, string message, params object[] args)
    {
        _logOperationLimitProvider.CheckLog(exception, message, args);
        List<object> argList = new List<object>();
        argList.Add(_blockProcessingContext.ChainId);
        argList.AddRange(args.ToList());
        _logger.Log(logLevel, AeFinderApplicationConsts.AppLogEventId, exception, "[{ChainId}] " + message,
            argList.ToArray());
    }
}