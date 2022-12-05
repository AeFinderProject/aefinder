using System.Collections.Generic;
using Volo.Abp.EventBus;

namespace AElfScan.Etos;

[EventName("AElf.NewBlocks")]
public class NewBlocksEto
{
    public List<NewBlockEto> NewBlocks { get; set; }
}