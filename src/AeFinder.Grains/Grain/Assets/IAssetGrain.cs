using AeFinder.Assets;
using AeFinder.Grains.State.Assets;

namespace AeFinder.Grains.Grain.Assets;

public interface IAssetGrain : IGrainWithGuidKey
{
    Task<AssetState> CreateAssetAsync(Guid id, Guid organizationId, CreateAssetInput input);
    Task<AssetState> GetAsync();
    Task PayAsync(decimal paidAmount);
    Task RelateAppAsync(string appId);
    Task SuspendAsync();
    Task StartUsingAsync(DateTime endTime);
    Task ReleaseAsync(DateTime beginTime);
}