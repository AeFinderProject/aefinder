using System;
using Xunit;

namespace AeFinder.App.OperationLimits;

public class LogOperationLimitProviderTests: AeFinderAppTestBase
{
    private readonly IOperationLimitManager _operationLimitManager;
    private readonly ILogOperationLimitProvider _logOperationLimitProvider;
    
    public LogOperationLimitProviderTests()
    {
        _operationLimitManager = GetRequiredService<IOperationLimitManager>();
        _logOperationLimitProvider = GetRequiredService<ILogOperationLimitProvider>();
    }
    
    [Fact]
    public void CheckLogTest()
    {
        for (var i = 0; i < 3; i++)
        {
            _logOperationLimitProvider.CheckLog(null, "Test");
        }
        
        Assert.Throws<OperationLimitException>(() => _logOperationLimitProvider.CheckLog(null, "Test"));
        
        _operationLimitManager.ResetAll();
        _logOperationLimitProvider.CheckLog(null, "Test");
        
        var message = "0123456789-";
        Assert.Throws<OperationLimitException>(() => _logOperationLimitProvider.CheckLog(new Exception(message), null));
        Assert.Throws<OperationLimitException>(() => _logOperationLimitProvider.CheckLog(null, message));
        Assert.Throws<OperationLimitException>(() => _logOperationLimitProvider.CheckLog(null, null,message));

    }
}