namespace AElfScan.Grains.State.BlockScan;

public class ClientState
{
    public ClientInfo ClientInfo { get; set; } = new();
    public SubscribeInfo SubscribeInfo {get;set;}= new();
}