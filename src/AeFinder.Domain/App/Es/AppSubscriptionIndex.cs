using AeFinder.Entities;
using AElf.EntityMapping.Entities;
using Nest;

namespace AeFinder.App.Es;

public class AppSubscriptionIndex: AeFinderEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return Version; }
    }
    [Keyword] public string AppId { get; set; }
    [Keyword] public string Version { get; set; }
    public SubscriptionManifestInfo SubscriptionManifest { get; set; }
    public SubscriptionStatus SubscriptionStatus { get; set; }
}