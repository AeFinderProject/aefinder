using AeFinder.Sdk.Entities;
using Nest;

namespace TokenApp.Entities;

public class Account: AeFinderEntity, IAeFinderEntity
{
    [Keyword] public string Address { get; set; }
    [Keyword] public string Symbol { get; set; }
    [Keyword] public long Amount { get; set; }
}