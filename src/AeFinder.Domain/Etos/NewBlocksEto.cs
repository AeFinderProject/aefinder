using System.Collections.Generic;
using Volo.Abp.EventBus;

namespace AeFinder.Etos;

[EventName("AElf.NewBlocks")]
public class NewBlocksEto
{
    public List<NewBlockEto> NewBlocks { get; set; }
}