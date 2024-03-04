using AElfIndexer.BlockScan;
using Orleans.Streams;

namespace AElfIndexer.App.Handlers;

public interface ISubscribedBlockHandler
{
    Task HandleAsync(SubscribedBlockDto blocks, StreamSequenceToken token = null);
}