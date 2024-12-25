using AeFinder.Assets;

namespace AeFinder.Grains.Grain.Assets;

public interface IAssetGrain : IGrainWithGuidKey
{
    Task CreateAssetAsync(Guid id, Guid organizationId, CreateAssetInput input);
    Task PayAsync(decimal paidAmount);
    Task RelateAppAsync(string appId);
    Task SuspendAsync();
    Task StartUsingAsync(DateTime endTime);
    Task ReleaseAsync(DateTime beginTime);
    Task UpdateQuantityAsync(long quantity, long replicas);
}