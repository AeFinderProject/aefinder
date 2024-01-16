using Google.Protobuf;

namespace AeFinder.Client;

public interface IAElfClientService
{
    Task<T> ViewAsync<T>(string chainId, string contractAddress, string methodName, IMessage parameter)
        where T : IMessage<T>, new();

    Task<T> ViewSystemAsync<T>(string chainId, string systemContractName, string methodName, IMessage parameter)
        where T : IMessage<T>, new();

}