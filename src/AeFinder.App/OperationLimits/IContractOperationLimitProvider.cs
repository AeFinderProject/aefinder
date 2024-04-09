using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AeFinder.App.OperationLimits;

public interface IContractOperationLimitProvider: IOperationLimitProvider
{
    void CheckCallCount();
}

public class ContractOperationLimitProvider : CallCountOperationLimitProvider, IContractOperationLimitProvider,
    ISingletonDependency
{
    private readonly OperationLimitOptions _options;

    public ContractOperationLimitProvider(IOptionsSnapshot<OperationLimitOptions> options)
    {
        _options = options.Value;
    }

    public void CheckCallCount()
    {
        CallCount++;

        if (CallCount > _options.MaxContractCallCount)
        {
            throw new ApplicationException(
                $"Too many contract calls. The maximum of calls allowed is {_options.MaxContractCallCount} per block.");
        }
    }
}