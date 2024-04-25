using Volo.Abp.DependencyInjection;

namespace AeFinder.App.BlockProcessing;

public interface IBlockProcessingContext
{
    public string ChainId { get; }
    public string BlockHash { get; }
    public long BlockHeight { get; }
    public DateTime BlockTime { get; }
    public bool IsRollback { get; }

    void SetContext(string chainId, string blockHash, long blockHeight, DateTime blockTime,
        bool isRollback);
}

public class BlockProcessingContext : IBlockProcessingContext,ISingletonDependency
{
    public string ChainId { get; private set; }
    public string BlockHash { get; private set; }
    public long BlockHeight { get; private set; }
    public DateTime BlockTime { get; private set; }
    public bool IsRollback { get; private set; }

    public void SetContext(string chainId, string blockHash, long blockHeight, DateTime blockTime,
        bool isRollback)
    {
        ChainId = chainId;
        BlockHash = blockHash;
        BlockHeight = blockHeight;
        BlockTime = blockTime;
        IsRollback = isRollback;
    }
}

