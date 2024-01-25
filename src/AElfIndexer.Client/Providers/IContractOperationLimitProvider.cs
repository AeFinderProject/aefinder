using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Client.Providers;

public interface IContractOperationLimitProvider: IOperationLimitProvider
{
    void CheckCallCount();
}

public class ContractOperationLimitProvider : CallCountOperationLimitProvider,IContractOperationLimitProvider,ISingletonDependency
{
    private readonly OperationLimitOptions _options;

    public ContractOperationLimitProvider(IOptionsSnapshot<OperationLimitOptions> options)
    {
        _options = options.Value;
    }

    public void CheckCallCount()
    {
        if (CallCount > _options.MaxContractCallCount)
        {
            throw new ApplicationException("Too many calls");
        }

        CallCount++;
    }
}