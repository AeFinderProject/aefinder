using System.Threading.Tasks;

namespace AeFinder.ApiKeys;

public interface IApiQueryPriceProvider
{
    Task<decimal> GetPriceAsync();
}