using AElfIndexer.Etos;
using AElfIndexer.Grains.EventData;

namespace AElfIndexer;

public class NewBlockTaskEntity
{
    public NewBlockEto newBlockEto { get; set; }
    public BlockData BlockData { get; set; }
}