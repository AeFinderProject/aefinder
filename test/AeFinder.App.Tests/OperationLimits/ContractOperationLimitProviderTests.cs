using System;
using Xunit;

namespace AeFinder.App.OperationLimits;

public class ContractOperationLimitProviderTests : AeFinderAppTestBase
{
    private readonly IOperationLimitManager _operationLimitManager;
    private readonly IContractOperationLimitProvider _contractOperationLimitProvider;

    public ContractOperationLimitProviderTests()
    {
        _operationLimitManager = GetRequiredService<IOperationLimitManager>();
        _contractOperationLimitProvider = GetRequiredService<IContractOperationLimitProvider>();
    }

    [Fact]
    public void CheckCallCountTest()
    {
        _contractOperationLimitProvider.CheckCallCount();
        _contractOperationLimitProvider.CheckCallCount();
        _contractOperationLimitProvider.CheckCallCount();
        
        Assert.Throws<ApplicationException>(() => _contractOperationLimitProvider.CheckCallCount());
        
        _operationLimitManager.ResetAll();
        _contractOperationLimitProvider.CheckCallCount();
    }
}