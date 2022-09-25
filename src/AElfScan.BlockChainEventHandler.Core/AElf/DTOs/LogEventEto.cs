namespace AElfScan.AElf.DTOs;

public class LogEventEto
{
    public string ContractAddress { get; set; }
    
    public string EventName { get; set; }
    
    /// <summary>
    /// 事件在交易内的排序位置
    /// </summary>
    public int Index { get; set; }
    
    public Dictionary<string, string> ExtraProperties {get;set;}
}