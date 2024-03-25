using System.Collections.Generic;
using Volo.Abp.EventBus;

namespace AeFinder.Etos;

[EventName("AElf.ConfirmBlocks")]
public class ConfirmBlocksEto
{
    public List<ConfirmBlockEto> ConfirmBlocks { get; set; }
    
}