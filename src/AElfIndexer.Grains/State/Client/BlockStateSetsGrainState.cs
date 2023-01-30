namespace AElfIndexer.Grains.State.Client;

public class BlockStateSetsGrainState<T>
{
    // BlockHash -> BlockStateSet
    public Dictionary<string, BlockStateSet<T>> BlockStateSets { get; set; }= new ();
    
    public BlockStateSet<T> LongestChainBlockStateSet { get; set; }
    
    public BlockStateSet<T> BestChainBlockStateSet { get; set; }

    public BlockStateSet<T> CurrentBlockStateSet { get; set; } = new();

    public Dictionary<string, string> LongestChainHashes { get; set; } = new ();

    public bool HasFork { get; set; }
}