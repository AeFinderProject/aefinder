using System;
using AeFinder.Apps;
using AeFinder.Entities;
using AElf.EntityMapping.Entities;
using Nest;

namespace AeFinder.App.ES;

public class AppLogIndex : AeFinderEntity<string>, IEntityMappingEntity
{
    [Keyword] public override string Id { get; set; }

    public DateTime @Timestamp { get; set; }

    public string Environment { get; set; }

    public string Message { get; set; }

    public FileBeatLogHostInfo Host { get; set; }

    public FileBeatLogAgentInfo Agent { get; set; }

    public AppLogInfo App_log { get; set; }
}

