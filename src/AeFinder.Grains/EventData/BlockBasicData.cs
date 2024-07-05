namespace AeFinder.Grains.EventData;

[GenerateSerializer]
public class BlockBasicData
{
    [Id(0)] public string ChainId { get; set; }
    [Id(1)] public string BlockHash { get; set; }
    [Id(2)] public long BlockHeight { get; set; }
    [Id(3)] public string PreviousBlockHash { get; set; }
    [Id(4)] public DateTime BlockTime { get; set; }
    [Id(5)] public bool Confirmed{get;set;}
}