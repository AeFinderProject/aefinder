using AeFinder.Client.Providers;
using AElf;
using AElf.Client;
using AElf.Client.Dto;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AeFinder.Client;

public class AElfClientService : IAElfClientService, ITransientDependency
{
    private readonly IAElfClientProvider _clientProvider;

    public AElfClientService(IAElfClientProvider clientProvider)
    {
        _clientProvider = clientProvider;
    }

    public async Task<T> ViewAsync<T>(string chainId, string contractAddress, string methodName, IMessage parameter) where T : IMessage<T>, new()
    {
        var client = _clientProvider.GetClient(chainId);
        var tx = new TransactionBuilder(client)
            .UseContract(contractAddress)
            .UseMethod(methodName)
            .UseParameter(parameter)
            .Build();
        return await PerformViewAsync<T>(client, tx);
    }

    public async Task<T> ViewSystemAsync<T>(string chainId, string systemContractName, string methodName, IMessage parameter) where T : IMessage<T>, new()
    {
        var client = _clientProvider.GetClient(chainId);
        var tx = new TransactionBuilder(client)
            .UseSystemContract(systemContractName)
            .UseMethod(methodName)
            .UseParameter(parameter)
            .Build();
        
        return await PerformViewAsync<T>(client, tx);
    }

    private async Task<T> PerformViewAsync<T>(AElfClient client, Transaction tx) where T : IMessage<T>, new()
    {
        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = tx.ToByteArray().ToHex()
        });
        var value = new T();
        value.MergeFrom(ByteArrayHelper.HexStringToByteArray(result));
        return value;
    }
}