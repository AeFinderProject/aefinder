using System;
using System.Collections.Generic;
using AeFinder.Entities;
using AElf.EntityMapping.Entities;
using Nest;

namespace AeFinder.App.Es;

public class AppResourceUsageIndex : AeFinderDomainEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id => AppInfo.AppId;

    public AppInfoImmutableIndex AppInfo { get; set; }
    
    [Keyword] 
    public Guid OrganizationId { get; set; }

    // Version -> ResourceUsage
    public Dictionary<string, List<ResourceUsageIndex>> ResourceUsages { get; set; }
}

public class ResourceUsageIndex
{
    public string Name { get; set; }
    public string Limit { get; set; }
    public string Usage { get; set; }
}