namespace AElfIndexer.Grains.State.Client;

public class BlockStateSetBucketState<T>
{
    // BlockHash -> BlockStateSet
    public Dictionary<string, BlockStateSet<T>> BlockStateSets { get; set; }= new ();
}