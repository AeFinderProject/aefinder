using AeFinder.Grains.Grain.Orders;
using AeFinder.Grains.State.Orders;

namespace AeFinder.Grains.Grain.Assets;

public class AssetOrderHandler : IOrderHandler
{
    private readonly IClusterClient _clusterClient;

    public AssetOrderHandler(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public async Task HandleOrderCreatedAsync(OrderState orderState)
    {
        foreach (var detail in orderState.Details)
        {
            if (detail.OriginalAsset != null)
            {
                var grain = _clusterClient.GetGrain<IAssetGrain>(detail.OriginalAsset.Id);
                await grain.LockAsync(true);
            }
        }
    }
}