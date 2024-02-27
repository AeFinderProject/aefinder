using AElfIndexer.BlockScan;
using Orleans.Streams;

namespace AElfIndexer.Client.BlockHandlers;

public interface ISubscribedBlockHandler
{
    Task HandleAsync(SubscribedBlockDto blocks, StreamSequenceToken token = null);
}