namespace AElfIndexer.Grains.State.Client;

public class BlockStateSetBucketState
{
    public Dictionary<string, Dictionary<string, AppBlockStateSet>> BlockStateSets { get; set; }= new ();
}