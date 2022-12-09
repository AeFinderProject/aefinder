using AElf.Client;
using AElf.CSharp.Core;
using Google.Protobuf;

namespace AElfIndexer.Client;

public static class AElfClientServiceExtensions
{
    public static async Task<T> ViewAsync<T>(this IAElfClientService clientService, string contractAddress,string methodName,IMessage parameter) where T : IEvent<T>, new()
    {
        var result = await clientService.ViewAsync(contractAddress, methodName, parameter, "Default");
        var value = new T();
        value.MergeFrom(result);
        return value;
    }
    
    public static async Task<T> ViewSystemAsync<T>(this IAElfClientService clientService, string systemContractName,string methodName,IMessage parameter) where T : IEvent<T>, new()
    {
        var result = await clientService.ViewSystemAsync(systemContractName, methodName, parameter, "Default");
        var value = new T();
        value.MergeFrom(result);
        return value;
    }
}