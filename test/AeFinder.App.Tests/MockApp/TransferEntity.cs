using AeFinder.Sdk;
using Nest;

namespace AeFinder.App.MockApp;

public class TransferEntity : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public string Symbol { get; set; }
    [Keyword] public string FromAccount { get; set; }
    [Keyword] public string ToAccount { get; set; }
    [Keyword] public long Amount { get; set; }
}