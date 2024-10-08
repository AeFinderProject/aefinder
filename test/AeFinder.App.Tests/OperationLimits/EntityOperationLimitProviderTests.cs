using System;
using Xunit;

namespace AeFinder.App.OperationLimits;

public class EntityOperationLimitProviderTests : AeFinderAppTestBase
{
    private readonly IOperationLimitManager _operationLimitManager;
    private readonly IEntityOperationLimitProvider _entityOperationLimitProvider;
    
    public EntityOperationLimitProviderTests()
    {
        _operationLimitManager = GetRequiredService<IOperationLimitManager>();
        _entityOperationLimitProvider = GetRequiredService<IEntityOperationLimitProvider>();
    }

    [Fact]
    public void CheckCallCountTest()
    {
        var entity = new {Name = "Test"};
        for (var i = 0; i < 100; i++)
        {
            _entityOperationLimitProvider.Check(entity);
        }
        
        Assert.Throws<OperationLimitException>(() => _entityOperationLimitProvider.Check(entity));
        
        _operationLimitManager.ResetAll();
        _entityOperationLimitProvider.Check(entity);

        var name = "Test";
        for (var i = 0; i < 1000; i++)
        {
            name += "0123456789";
        }
        entity = new {Name = name};
        Assert.Throws<OperationLimitException>(() => _entityOperationLimitProvider.Check(entity));

    }
}