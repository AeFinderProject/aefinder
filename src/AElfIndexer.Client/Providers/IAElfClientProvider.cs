using AElf.Client;

namespace AElfIndexer.Client.Providers;

public interface IAElfClientProvider
{
    AElfClient GetClient(string chainId);
}