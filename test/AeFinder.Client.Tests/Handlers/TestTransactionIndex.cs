using System;
using System.Collections.Generic;
using AeFinder.Grains.State.Client;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace AeFinder.Client.Handlers;

public class TestTransactionIndex : AeFinderClientEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    public DateTime BlockTime { get; set; }
    [Keyword] public string From { get; set; }
    [Keyword] public string To { get; set; }
    [Keyword] public string MethodName { get; set; }
    public bool Confirmed { get; set; }
    public List<LogEventInfo> LogEventInfos { get; set; }
}