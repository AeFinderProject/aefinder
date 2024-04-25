using AeFinder.Etos;
using AeFinder.Grains.EventData;

namespace AeFinder.BlockChainEventHandler;

public class NewBlockTaskEntity
{
    public NewBlockEto newBlockEto { get; set; }
    public BlockData BlockData { get; set; }
}