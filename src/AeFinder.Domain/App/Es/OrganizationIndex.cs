using System.Collections.Generic;
using AeFinder.Entities;
using AElf.EntityMapping.Entities;
using Nest;

namespace AeFinder.App.Es;

public class OrganizationIndex : AeFinderEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return OrganizationId; }
    }

    [Keyword] public string OrganizationId { get; set; }
    [Keyword] public string OrganizationName { get; set; }
    public int MaxAppCount { get; set; }
    public List<string> AppIds { get; set; }
    public int Status { get; set; }
}