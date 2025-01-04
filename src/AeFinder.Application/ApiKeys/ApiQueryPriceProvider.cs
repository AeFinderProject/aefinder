using System;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Merchandises;
using AElf.EntityMapping.Repositories;
using Volo.Abp.DependencyInjection;

namespace AeFinder.ApiKeys;

public class ApiQueryPriceProvider : IApiQueryPriceProvider, ISingletonDependency
{
    private readonly IEntityMappingRepository<MerchandiseIndex, Guid> _merchandiseIndexRepository;

    private decimal _price;

    public ApiQueryPriceProvider(IEntityMappingRepository<MerchandiseIndex, Guid> merchandiseIndexRepository)
    {
        _merchandiseIndexRepository = merchandiseIndexRepository;
    }

    public async Task<decimal> GetPriceAsync()
    {
        if (_price == 0)
        {
            var queryable = await _merchandiseIndexRepository.GetQueryableAsync();
            var apiQuery = queryable.First(o =>
                o.Status == (int)MerchandiseStatus.Listed && o.Type == (int)MerchandiseType.ApiQuery);

            _price = apiQuery.Price;
        }

        return _price;
    }
}