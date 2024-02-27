using AElf.Client;

namespace AElfIndexer.Client.BlockChain;

public interface IAElfClientProvider
{
    AElfClient GetClient(string chainId);
}