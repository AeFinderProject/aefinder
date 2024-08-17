using AeFinder.Entities;
using AElf.EntityMapping.Entities;
using Nest;

namespace AeFinder.App.Es;

public class AppSubscriptionPodIndex: AeFinderEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return Version; }
    }
    [Keyword] public string AppId { get; set; }
    [Keyword] public string Version { get; set; }
    [Keyword] public string DockerImage { get; set; }
}