namespace AElfScan.AElf.DTOs;

public class TransactionEto
{
    public string TransactionId { get; set; }
    
    public string From { get; set; }
    
    public string To { get; set; }
    
    public string MethodName { get; set; }
    
    public string Params { get; set; }
    
    public string Signature { get; set; }
    
    /// <summary>
    /// 交易在区块内的排序位置
    /// </summary>
    public int Index{get;set;}
    
    public int Status { get; set; }
    
    public Dictionary<string, string> ExtraProperties {get;set;}

    public List<LogEventEto> LogEvents { get; set; } = new List<LogEventEto>();
}