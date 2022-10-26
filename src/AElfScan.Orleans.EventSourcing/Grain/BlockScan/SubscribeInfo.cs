namespace AElfScan.Orleans.EventSourcing.Grain.BlockScan;

public class SubscribeInfo
{
    public string ChainId { get; set; }
    public long StartBlockNumber{get;set;}
    public bool OnlyConfirmedBlock { get; set; }
    public List<SubscribeEvent> SubscribeEvents {get;set;}= new();
}

public class SubscribeEvent
{
    public string ContractAddress { get; set; }
    public List<string> EventNames { get; set; }= new();
}