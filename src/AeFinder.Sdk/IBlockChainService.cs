using Google.Protobuf;

namespace AeFinder.Sdk;

public interface IBlockChainService
{
    Task<T> ViewContractAsync<T>(string chainId, string contractAddress, string methodName, IMessage parameter)
        where T : IMessage<T>, new();

}