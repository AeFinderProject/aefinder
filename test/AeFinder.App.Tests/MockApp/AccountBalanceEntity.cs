using AeFinder.Sdk.Entities;
using Nest;

namespace AeFinder.App.MockApp;

public class AccountBalanceEntity: AeFinderEntity, IAeFinderEntity
{
    [Keyword] public string Account { get; set; }
    [Keyword] public string Symbol { get; set; }
    [Keyword] public long Amount { get; set; }
}