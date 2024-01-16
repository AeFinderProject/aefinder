using AeFinder.BlockScan;
using Orleans.Streams;

namespace AeFinder.Client.Handlers;

public interface ISubscribedBlockHandler
{
    Task HandleAsync(SubscribedBlockDto blocks, StreamSequenceToken token = null);
}