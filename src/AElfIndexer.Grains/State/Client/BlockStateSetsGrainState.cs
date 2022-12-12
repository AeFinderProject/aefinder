namespace AElfIndexer.Grains.State.Client;

public class BlockStateSetsGrainState<T>
{
    // BlockHash -> BlockStateSet
    public Dictionary<string, BlockStateSet<T>> BlockStateSets { get; set; }= new ();

    public BlockStateSet<T> CurrentBlockStateSet { get; set; } = new();

    public Dictionary<string, string> BestChainHashes { get; set; } = new ();

    public bool HasFork { get; set; }
}