using AeFinder.Block.Dtos;
using AeFinder.Grains.State.BlockStates;

namespace AeFinder.Grains.Grain.BlockStates;

public class BlockStateSet
{
    public BlockWithTransactionDto Block { get; set; }
    public Dictionary<string, AppState> Changes { get; set; } = new ();
    public bool Processed { get; set; }
}