using AElf.Client;

namespace AeFinder.App.BlockChain;

public interface IAElfClientProvider
{
    AElfClient GetClient(string chainId);
}