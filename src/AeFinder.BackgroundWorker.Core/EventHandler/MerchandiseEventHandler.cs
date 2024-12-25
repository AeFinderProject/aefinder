using AeFinder.Merchandises;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class MerchandiseEventHandler: 
    IDistributedEventHandler<MerchandiseChangedEto>, 
    ITransientDependency
{
    private readonly IMerchandiseService _merchandiseService;

    public MerchandiseEventHandler(IMerchandiseService merchandiseService)
    {
        _merchandiseService = merchandiseService;
    }

    public async Task HandleEventAsync(MerchandiseChangedEto eventData)
    {
        await _merchandiseService.AddOrUpdateIndexAsync(eventData);
    }
}