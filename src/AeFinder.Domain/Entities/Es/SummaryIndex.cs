using AElf.EntityMapping.Entities;
using Nest;

namespace AeFinder.Entities.Es;

public class SummaryIndex : AeFinderEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return ChainId; }
    }

    [Keyword] public string ChainId { get; set; }
    public long LatestBlockHeight { get; set; }
    [Keyword] public string LatestBlockHash { get; set; }
    public long ConfirmedBlockHeight { get; set; }
    [Keyword] public string ConfirmedBlockHash { get; set; }
}