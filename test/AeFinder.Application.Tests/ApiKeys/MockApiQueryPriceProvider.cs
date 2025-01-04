using System.Threading.Tasks;

namespace AeFinder.ApiKeys;

public class MockApiQueryPriceProvider : IApiQueryPriceProvider
{
    public async Task<decimal> GetPriceAsync()
    {
        return 0.00004M;
    }
}