namespace AElfScan.DTOs;

public class BlockEto
{
    public string BlockHash { get; set; }
    public long BlockNumber { get; set; }
    public string PreviousBlockHash { get; set; }
    public DateTime BlockTime { get; set; }
    public string SignerPubkey { get; set; }
    public string Signature { get; set; }
    public Dictionary<string, string> ExtraProperties {get;set;}
    public List<TransactionEto> Transactions { get; set; } = new List<TransactionEto>();
}