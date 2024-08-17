using AeFinder.Entities;
using AElf.EntityMapping.Entities;
using Nest;

namespace AeFinder.App.Es;

public class AppLimitInfoIndex : AeFinderEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return AppId; }
    }
    [Keyword] public string OrganizationId { get; set; }
    [Keyword] public string OrganizationName { get; set; }
    [Keyword] public string AppId { get; set; }
    [Keyword] public string AppName { get; set; }
    public ResourceLimitInfo ResourceLimit { get; set; }
    public OperationLimitInfo OperationLimit { get; set; }
}