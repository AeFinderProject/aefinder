namespace AeFinder.Grains.State.Client;

public class BlockStateSetInfoGrainState
{
    public Dictionary<BlockFilterType, long> BlockHeightInfo { get; set; } = new();
}