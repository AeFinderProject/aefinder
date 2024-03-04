using System;
using Xunit;

namespace AElfIndexer.App.OperationLimits;

public class LogOperationLimitProviderTests: AElfIndexerAppTestBase
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
        
        Assert.Throws<ApplicationException>(() => _logOperationLimitProvider.CheckLog(null, "Test"));
        
        _operationLimitManager.ResetAll();
        _logOperationLimitProvider.CheckLog(null, "Test");
        
        var message = "0123456789-";
        Assert.Throws<ApplicationException>(() => _logOperationLimitProvider.CheckLog(new Exception(message), null));
        Assert.Throws<ApplicationException>(() => _logOperationLimitProvider.CheckLog(null, message));
        Assert.Throws<ApplicationException>(() => _logOperationLimitProvider.CheckLog(null, null,message));

    }
}