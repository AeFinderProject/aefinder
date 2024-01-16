using AElf.Client;

namespace AeFinder.Client.Providers;

public interface IAElfClientProvider
{
    AElfClient GetClient(string chainId);
    
    bool TryAddClient(string chainId, string endpoint);

    void RemoveClient(string chainId);
}