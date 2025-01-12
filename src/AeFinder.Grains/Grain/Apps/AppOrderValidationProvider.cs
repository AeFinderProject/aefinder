using AeFinder.Assets;
using AeFinder.Grains.Grain.Orders;
using AeFinder.Grains.State.Orders;

namespace AeFinder.Grains.Grain.Apps;

public class AppOrderValidationProvider : IOrderValidationProvider
{
    public async Task<bool> ValidateBeforeOrderAsync(OrderState orderState)
    {
        foreach (var detail in orderState.Details)
        {
            if (detail.OriginalAsset != null)
            {
                if (detail.OriginalAsset.IsLocked || 
                    detail.OriginalAsset.Status == AssetStatus.Pending ||
                    detail.OriginalAsset.Status == AssetStatus.Released)
                {
                    return false;
                }
            }
        }

        return true;
    }
}