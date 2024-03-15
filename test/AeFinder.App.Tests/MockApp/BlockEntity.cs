using AeFinder.Sdk;
using Nest;

namespace AeFinder.App.MockApp;

public class BlockEntity : AeFinderEntity, IAeFinderEntity
{
    [Keyword] public string BlockHash { get; set; }
    [Keyword] public string Miner { get; set; }
}