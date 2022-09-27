using System.Collections.Generic;
using Volo.Abp.EventBus;

namespace AElfScan.AElf.Etos;

[EventName("AElf.ConfirmBlocks")]
public class ConfirmBlocksEto
{
    public List<ConfirmBlockEto> ConfirmBlocks { get; set; }
}