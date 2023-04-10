using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using AElfIndexer.Grains.State.Client;
using Nest;

namespace AElfIndexer.Client.Handlers;

public class TestTransactionIndex : AElfIndexerClientEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    public DateTime BlockTime { get; set; }
    [Keyword] public string From { get; set; }
    [Keyword] public string To { get; set; }
    [Keyword] public string MethodName { get; set; }
    public bool Confirmed { get; set; }
    public List<LogEventInfo> LogEventInfos { get; set; }
}