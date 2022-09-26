using Volo.Abp.EventBus;

namespace AElfScan.AElf.Etos;

[EventName("AElf.ConfirmBlock")]
public class ConfirmBlockEto
{
    public string ChainId { get; set; }
    public string BlockHash { get; set; }
    public long BlockNumber { get; set; }
    public string PreviousBlockHash { get; set; }
    public bool IsConfirmed{get;set;}
}