namespace AeFinder.Options;

public class ContractOptions
{
    public int DelaySeconds { get; set; } = 3;
    public int ResultQueryRetryTimes { get; set; } = 10;
    public string SideChainNodeBaseUrl { get; set; }
    public string BillingContractAddress { get; set; }
    public string BillingContractChainId { get; set; }
    public string TreasurerAccountPrivateKeyForCallTx { get; set; }
}