using System.Collections.Generic;

namespace AElfScan.ScanClients;

public class SubscribeInfo
{
    public long StartBlockNumber{get;set;}
    public List<SubscribeEvent> SubscribeEvents {get;set;}
}

public class SubscribeEvent
{
    public string ContractAddress { get; set; }
    public List<string> EventNames { get; set; }
}