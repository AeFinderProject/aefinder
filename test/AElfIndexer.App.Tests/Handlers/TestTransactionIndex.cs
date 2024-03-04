using System;
using System.Collections.Generic;
using AElfIndexer.Sdk;
using JetBrains.Annotations;
using Nest;

namespace AElfIndexer.App.Handlers;

public class TestTransactionIndex : IndexerEntity, IIndexerEntity
{
    public DateTime BlockTime { get; set; }
    [Keyword] public string From { get; set; }
    [Keyword] public string To { get; set; }
    [Keyword] public string MethodName { get; set; }
    public bool Confirmed { get; set; }
    public List<LogEvent> LogEventInfos { get; set; }

    public TestTransactionIndex([NotNull] string id) : base(id)
    {
    }
}