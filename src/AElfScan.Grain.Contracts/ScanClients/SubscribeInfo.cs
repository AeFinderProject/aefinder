namespace AElfScan.Grain.Contracts.ScanClients;

public class SubscribeInfo
{
    public long StartBlockNumber{get;set;}
    public List<SubscribeEvent> SubscribeEvents {get;set;}= new();
}

public class SubscribeEvent
{
    public string ContractAddress { get; set; }
    public List<string> EventNames { get; set; }= new();
}