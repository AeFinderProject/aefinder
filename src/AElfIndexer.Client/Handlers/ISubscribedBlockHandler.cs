using AElfIndexer.BlockScan;
using Orleans.Streams;

namespace AElfIndexer.Client.Handlers;

public interface ISubscribedBlockHandler<T>
{
    Task HandleAsync(SubscribedBlockDto blocks, StreamSequenceToken token = null);
}