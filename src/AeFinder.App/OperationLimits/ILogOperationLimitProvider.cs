using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AeFinder.App.OperationLimits;

public interface ILogOperationLimitProvider: IOperationLimitProvider
{
    void CheckLog(Exception exception, string message, params object[] args);
}

public class LogOperationLimitProvider : CallCountOperationLimitProvider, ILogOperationLimitProvider,
    ISingletonDependency
{
    private readonly OperationLimitOptions _options;

    public LogOperationLimitProvider(IOptionsSnapshot<OperationLimitOptions> options)
    {
        _options = options.Value;
    }

    public void CheckLog(Exception exception, string message, params object[] args)
    {
        CallCount++;

        if (CallCount > _options.MaxLogCallCount)
        {
            throw new ApplicationException(
                $"Too many log calls. The maximum of calls allowed is {_options.MaxLogCallCount} per block.");
        }

        var size = 0;
        if (exception != null)
        {
            size += exception.Message.Length;
        }

        if (message != null)
        {
            size += message.Length;
        }

        foreach (var arg in args)
        {
            size += arg.ToString()!.Length;
        }

        if (size > _options.MaxLogSize)
        {
            throw new ApplicationException(
                $"Too large log. The log {size} exceeds the maximum value {_options.MaxLogSize}");
        }
    }
}