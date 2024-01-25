using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Client.Providers;

public class CallCountOperationLimitProvider : IOperationLimitProvider
{
    protected int CallCount;

    public void Reset()
    {
        CallCount = 0;
    }
}