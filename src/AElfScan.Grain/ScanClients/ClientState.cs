using AElfScan.Grain.Contracts.ScanClients;

namespace AElfScan.Grain.ScanClients;

public class ClientState
{
    public ClientInfo ClientInfo { get; set; } = new();
    public SubscribeInfo SubscribeInfo {get;set;}= new();
}