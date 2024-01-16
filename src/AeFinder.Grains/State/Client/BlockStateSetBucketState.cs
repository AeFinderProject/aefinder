namespace AeFinder.Grains.State.Client;

public class BlockStateSetBucketState<T>
{
    public Dictionary<string, Dictionary<string, BlockStateSet<T>>> BlockStateSets { get; set; }= new ();
}