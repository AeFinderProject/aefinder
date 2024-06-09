using AeFinder.Block.Dtos;
using AeFinder.BlockScan;

namespace AeFinder.Grains.Grain.BlockStates;

public class BlockStateSet
{
    public BlockWithTransactionDto Block { get; set; }
    public Dictionary<string, string> Changes { get; set; } = new ();
    public bool Processed { get; set; }
}