using System.Collections.Generic;
using Volo.Abp.EventBus;

namespace AeFinder.Etos;

[EventName("ConfirmBlocks")]
public class ConfirmBlocksEto
{
    public List<ConfirmBlockEto> ConfirmBlocks { get; set; }
    
}