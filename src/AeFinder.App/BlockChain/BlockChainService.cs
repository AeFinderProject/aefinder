using AeFinder.App.OperationLimits;
using AElf;
using AElf.Client;
using AElf.Client.Dto;
using AeFinder.Sdk;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;
using Transaction = AElf.Types.Transaction;

namespace AeFinder.App.BlockChain;

public class BlockChainService : IBlockChainService, ITransientDependency
{
    private readonly IAElfClientProvider _clientProvider;
    private readonly IContractOperationLimitProvider _contractOperationLimitProvider;

    public BlockChainService(IAElfClientProvider clientProvider,
        IContractOperationLimitProvider contractOperationLimitProvider)
    {
        _clientProvider = clientProvider;
        _contractOperationLimitProvider = contractOperationLimitProvider;
    }

    public async Task<T> ViewContractAsync<T>(string chainId, string contractAddress, string methodName,
        IMessage parameter) where T : IMessage<T>, new()
    {
        _contractOperationLimitProvider.CheckCallCount();   
        var client = _clientProvider.GetClient(chainId);
        var tx = new TransactionBuilder(client)
            .UseContract(contractAddress)
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