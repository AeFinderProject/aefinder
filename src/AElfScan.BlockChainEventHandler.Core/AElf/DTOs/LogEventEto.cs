namespace AElfScan.AElf.DTOs;

public class LogEventEto
{
    public string ContractAddress { get; set; }
    
    public string EventName { get; set; }
    
    /// <summary>
    /// The ranking position of the event within the transaction
    /// </summary>
    public int Index { get; set; }
    
    public Dictionary<string, string> ExtraProperties {get;set;}
}