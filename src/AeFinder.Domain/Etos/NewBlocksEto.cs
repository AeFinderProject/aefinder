using System.Collections.Generic;
using Volo.Abp.EventBus;

namespace AeFinder.Etos;

[EventName("NewBlocks")]
public class NewBlocksEto
{
    public List<NewBlockEto> NewBlocks { get; set; }
}