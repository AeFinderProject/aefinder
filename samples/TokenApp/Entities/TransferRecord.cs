using AElfIndexer.Sdk;
using Nest;

namespace TokenApp.Entities;

public class TransferRecord: IndexerEntity, IIndexerEntity
{
    [Keyword] public string Symbol { get; set; }
    [Keyword] public string FromAddress { get; set; }
    [Keyword] public string ToAddress { get; set; }
    [Keyword] public long Amount { get; set; }
}