using AeFinder.BlockScan;
using Orleans.Streams;

namespace AeFinder.App.Handlers;

public interface ISubscribedBlockHandler
{
    Task HandleAsync(SubscribedBlockDto blocks, StreamSequenceToken token = null);
}