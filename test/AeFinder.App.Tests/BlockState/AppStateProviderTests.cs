using System.Threading.Tasks;
using AeFinder.Grains.State.BlockStates;
using Shouldly;
using Xunit;

namespace AeFinder.App.BlockState;

public class AppStateProviderTests : AeFinderAppTestBase
{
    private readonly IAppStateProvider _appStateProvider;

    public AppStateProviderTests()
    {
        _appStateProvider = GetRequiredService<IAppStateProvider>();
    }

    [Fact]
    public async Task Test()
    {
        // var key = "dapp";
        // var chainId = "AELF";
        // for (int i = 0; i < 6; i++)
        // {
        //     await _appStateProvider.SetLastIrreversibleStateAsync(chainId,key+i,  new AppState
        //     {
        //         Type = "name,assembly",
        //         Value = i.ToString()
        //     });
        // }
        //
        // for (int i = 0; i < 6; i++)
        // {
        //     var value = await _appStateProvider.GetLastIrreversibleStateAsync<string>(chainId,key+i);
        //     value.ShouldBe(i.ToString());
        // }
    }
}