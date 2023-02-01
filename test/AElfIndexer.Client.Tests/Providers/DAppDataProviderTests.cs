using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AElfIndexer.Client.Providers;

public class DAppDataProviderTests : AElfIndexerClientTestBase
{
    private readonly IDAppDataProvider _dAppDataProvider;

    public DAppDataProviderTests()
    {
        _dAppDataProvider = GetRequiredService<IDAppDataProvider>();
    }

    [Fact]
    public async Task Test()
    {
        var key = "dapp";
        for (int i = 0; i < 6; i++)
        {
            await _dAppDataProvider.SetLibValueAsync(key+i, i.ToString());
        }
        
        for (int i = 0; i < 6; i++)
        {
            var value = await _dAppDataProvider.GetLibValueAsync<string>(key+i);
            value.ShouldBe(i.ToString());
        }

        await _dAppDataProvider.SaveDataAsync();
        
        for (int i = 0; i < 6; i++)
        {
            var value = await _dAppDataProvider.GetLibValueAsync<string>(key+i);
            value.ShouldBe(i.ToString());
        }
    }
}