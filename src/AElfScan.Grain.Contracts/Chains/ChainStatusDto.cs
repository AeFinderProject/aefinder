namespace AElfScan.Grain.Contracts.Chains;

public class ChainStatusDto
{
    public long BlockHeight { get; set; }
    public string BlockHash { get; set; }
    public long ConfirmBlockHeight { get; set; }
    public string ConfirmBlockHash { get; set; }
}