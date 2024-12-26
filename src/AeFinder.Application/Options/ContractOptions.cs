namespace AeFinder.Options;

public class ContractOptions
{
    public string SideChainNodeBaseUrl { get; set; }
    public string BillingContractAddress { get; set; }
    public string BillingContractChainId { get; set; }
    public string TreasurerAccountPrivateKeyForCallTx { get; set; }
}