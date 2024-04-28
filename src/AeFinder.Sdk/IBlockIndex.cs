namespace AeFinder.Sdk;

public interface IBlockIndex
{
    string BlockHash { get; }
    long BlockHeight { get; }
}

public class BlockIndex : IBlockIndex
{
    public string BlockHash { get; }
    public long BlockHeight { get; }

    public BlockIndex(string blockHash, long blockHeight)
    {
        BlockHash = blockHash;
        BlockHeight = blockHeight;
    }
}