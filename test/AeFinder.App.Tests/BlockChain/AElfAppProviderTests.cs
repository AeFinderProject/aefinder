using System.Collections.Generic;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AeFinder.App.BlockChain;

public class AElfAppProviderTests : AeFinderAppTestBase
{
    private readonly IAElfClientProvider _clientProvider;

    public AElfAppProviderTests()
    {
        _clientProvider = GetRequiredService<IAElfClientProvider>();
    }

    [Fact]
    public Task ClientTest()
    {
        var client = _clientProvider.GetClient("AELF");
        client.ShouldNotBeNull();

        client = _clientProvider.GetClient("tDVV");
        client.ShouldNotBeNull();
        
        Assert.Throws<KeyNotFoundException>(()=>_clientProvider.GetClient("tDVW"));
        return Task.CompletedTask;
    }
}