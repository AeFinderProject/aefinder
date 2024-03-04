using AElf.Client;

namespace AElfIndexer.App.BlockChain;

public interface IAElfClientProvider
{
    AElfClient GetClient(string chainId);
}