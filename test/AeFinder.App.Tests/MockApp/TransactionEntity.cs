using System;
using System.Collections.Generic;
using AeFinder.Sdk.Entities;
using AeFinder.Sdk.Processor;
using Nest;

namespace AeFinder.App.MockApp;

public class TransactionEntity : AeFinderEntity, IAeFinderEntity
{
    public DateTime BlockTime { get; set; }
    [Keyword] public string From { get; set; }
    [Keyword] public string To { get; set; }
    [Keyword] public string MethodName { get; set; }
    public bool Confirmed { get; set; }
    public List<LogEvent> LogEventInfos { get; set; }
}