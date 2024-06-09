using System.Threading.Tasks;
using AeFinder.BlockScan;

namespace AeFinder.Subscriptions;

public interface ISubscriptionAppService
{
    Task<string> AddSubscriptionAsync(string appId, SubscriptionManifestDto manifest, byte[] code);
    Task UpdateSubscriptionManifestAsync(string appId, string version, SubscriptionManifestDto manifest);
    Task UpdateSubscriptionCodeAsync(string appId, string version, byte[] code);
    Task<AllSubscriptionDto> GetSubscriptionManifestAsync(string appId);
}