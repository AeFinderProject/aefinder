using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.Orders;
using AeFinder.Grains.State.Orders;

namespace AeFinder.Grains.Grain.Assets;

public class AssetOrderValidationProvider : IOrderValidationProvider
{
    private readonly IClusterClient _clusterClient;

    public AssetOrderValidationProvider(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public async Task<bool> ValidateBeforeOrderAsync(OrderState orderState)
    {
        if (orderState.ExtraData.TryGetValue(AeFinderApplicationConsts.RelateAppExtraDataKey, out var appId))
        {
            var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(appId));
            var app = await appGrain.GetAsync();
            return !app.IsLocked;
        }

        return true;
    }
}