using AeFinder.Grains.Grain.Orders;
using AeFinder.Grains.State.Orders;

namespace AeFinder.Grains.Grain.Apps;

public class AppOrderHandler : IOrderHandler
{
    private readonly IClusterClient _clusterClient;

    public AppOrderHandler(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public async Task HandleOrderCreatedAsync(OrderState orderState)
    {
        if (orderState.ExtraData.TryGetValue(AeFinderApplicationConsts.RelateAppExtraDataKey, out var appId))
        {
            var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(appId));
            await appGrain.LockAsync(true);
        }
    }
}