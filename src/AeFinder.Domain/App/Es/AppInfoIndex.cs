using System;
using AeFinder.Apps;
using AeFinder.Entities;
using AElf.EntityMapping.Entities;
using Nest;

namespace AeFinder.App.Es;

public class AppInfoIndex : AeFinderEntity<string>, IEntityMappingEntity
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
    [Keyword] public string ImageUrl { get; set; }
    [Keyword] public string Description { get; set; }
    [Keyword] public string DeployKey { get; set; }
    [Keyword] public string SourceCodeUrl { get; set; }
    public AppStatus Status { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
    public AppVersionInfo Versions { get; set; }
    public DateTime DeleteTime { get; set; }
    public DateTime? DeployTime { get; set; }
    public bool IsLocked { get; set; }
}