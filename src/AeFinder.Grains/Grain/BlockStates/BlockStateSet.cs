using AeFinder.Block.Dtos;
using AeFinder.Grains.State.BlockStates;

namespace AeFinder.Grains.Grain.BlockStates;

[GenerateSerializer]
public class BlockStateSet
{
    [Id(0)]public BlockWithTransactionDto Block { get; set; }
    [Id(1)]public Dictionary<string, AppState> Changes { get; set; } = new ();
    [Id(2)]public bool Processed { get; set; }
}