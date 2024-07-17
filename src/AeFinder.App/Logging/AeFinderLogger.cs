using System.Collections.Concurrent;
using AeFinder.App.BlockProcessing;
using AeFinder.App.OperationLimits;
using AeFinder.Sdk.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Volo.Abp.DependencyInjection;

namespace AeFinder.App.Logging;

public class AeFinderLogger : IAeFinderLogger, ISingletonDependency
{
    // private readonly ILogger<AeFinderLogger> _logger;
    private readonly ILogOperationLimitProvider _logOperationLimitProvider;
    private readonly IBlockProcessingContext _blockProcessingContext;
    private readonly ConcurrentDictionary<string, ILogger<AeFinderLogger>> _loggers;
    private readonly AppInfoOptions _appInfoOptions;
    private readonly IConfiguration _configuration;

    public AeFinderLogger(IConfiguration configuration,
        IOptionsSnapshot<AppInfoOptions> appInfoOptions, ILogOperationLimitProvider logOperationLimitProvider,
        IBlockProcessingContext blockProcessingContext)
    {
        // _logger = logger;
        _appInfoOptions = appInfoOptions.Value;
        _logOperationLimitProvider = logOperationLimitProvider;
        _blockProcessingContext = blockProcessingContext;
        _configuration = configuration;
    }

    public void Log(LogLevel logLevel, Exception exception, string message, params object[] args)
    {
        // _logOperationLimitProvider.CheckLog(exception, message, args);
        // List<object> argList = new List<object>();
        // argList.Add(_blockProcessingContext.ChainId);
        // argList.AddRange(args.ToList());
        // _logger.Log(logLevel, AeFinderApplicationConsts.AppLogEventId, exception, "ChainId:{ChainId} " + message,
        //     argList.ToArray());

        if (!_loggers.TryGetValue(_blockProcessingContext.ChainId, out var logger))
        {
            logger = LoggerFactory.Create(c => new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("AppId", _appInfoOptions.AppId)
                    .Enrich.WithProperty("Version", _appInfoOptions.Version)
                    .Enrich.WithProperty("ChainId", _blockProcessingContext.ChainId)
                    .ReadFrom.Configuration(_configuration))
                .CreateLogger<AeFinderLogger>();
        }

        _logOperationLimitProvider.CheckLog(exception, message, args);
        logger.Log(logLevel,AeFinderApplicationConsts.AppLogEventId, exception, message, args);
    }
}