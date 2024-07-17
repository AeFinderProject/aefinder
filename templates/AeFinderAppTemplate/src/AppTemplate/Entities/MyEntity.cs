using AeFinder.Sdk.Entities;
using Nest;

namespace AppTemplate.Entities;

public class MyEntity: AeFinderEntity, IAeFinderEntity
{
    [Keyword] public string Address { get; set; }
    [Keyword] public string Symbol { get; set; }
    public long Amount { get; set; }
}