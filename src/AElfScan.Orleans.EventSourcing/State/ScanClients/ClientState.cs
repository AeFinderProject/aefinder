using AElfScan.Orleans.EventSourcing.Grain.ScanClients;

namespace AElfScan.Orleans.EventSourcing.State.ScanClients;

public class ClientState
{
    public ClientInfo ClientInfo { get; set; } = new();
    public SubscribeInfo SubscribeInfo {get;set;}= new();
}