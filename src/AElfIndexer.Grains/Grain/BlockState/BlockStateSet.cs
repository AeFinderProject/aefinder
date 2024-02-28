using AElfIndexer.Block.Dtos;

namespace AElfIndexer.Grains.Grain.BlockState;

public class BlockStateSet
{
    public BlockWithTransactionDto Block { get; set; }
    public Dictionary<string, string> Changes { get; set; } = new ();
    public bool Processed { get; set; }
}