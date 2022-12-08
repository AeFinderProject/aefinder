namespace AElfIndexer.Grains.State.Client;

public class BlockStateSetsGrainState<T>
{
    // BlockHash -> BlockStateSet
    public Dictionary<string, BlockStateSet<T>> BlockStateSets = new ();
    
    public BlockStateSet<T> CurrentBlockStateSet { get; set; }
    
    public Dictionary<string,string> BestChainHashes { get; set; }

    public bool HasFork { get; set; }
}