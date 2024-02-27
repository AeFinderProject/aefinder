using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElfIndexer.Client.BlockChain;
using Shouldly;
using Xunit;

namespace AElfIndexer.Client.Providers;

public class AElfClientProviderTests : AElfIndexerClientTestBase
{
    private readonly IAElfClientProvider _clientProvider;

    public AElfClientProviderTests()
    {
        _clientProvider = GetRequiredService<IAElfClientProvider>();
    }

    [Fact]
    public async Task ClientTest()
    {
        var client = _clientProvider.GetClient("AELF");
        client.ShouldNotBeNull();

        var addResult = _clientProvider.TryAddClient("tDVV", "http://sidechain.io");
        addResult.ShouldBeTrue();
        
        client = _clientProvider.GetClient("tDVV");
        client.ShouldNotBeNull();
        
        _clientProvider.RemoveClient("tDVV");
        Assert.Throws<KeyNotFoundException>(()=>_clientProvider.GetClient("tDVV"));
    }
}