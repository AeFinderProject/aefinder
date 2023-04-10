using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElfIndexer.Grains.State.Client;
using Microsoft.Extensions.Logging;
using Volo.Abp.ObjectMapping;

namespace AElfIndexer.Client.Handlers;

public class MockTokenTransferredTransactionProcessor : AElfLogEventProcessorBase<Transferred, TransactionInfo>
{
    private readonly IAElfIndexerClientEntityRepository<TestTransferredIndex, TransactionInfo> _transferredRepository;
    private readonly IObjectMapper _objectMapper;

    public MockTokenTransferredTransactionProcessor(
        ILogger<AElfLogEventProcessorBase<Transferred, TransactionInfo>> logger,
        IObjectMapper objectMapper,
        IAElfIndexerClientEntityRepository<TestTransferredIndex, TransactionInfo> transferredRepository)
        : base(logger)
    {
        _objectMapper = objectMapper;
        _transferredRepository = transferredRepository;
    }

    public override string GetContractAddress(string chainId)
    {
        return "TokenContractAddress";
    }

    protected override async Task HandleEventAsync(Transferred eventValue, LogEventContext context)
    {
        var index = new TestTransferredIndex
        {
            Id = context.TransactionId + context.Index + eventValue.Amount,
            FromAccount = eventValue.From.ToBase58(),
            ToAccount = eventValue.To.ToBase58(),
            Symbol = eventValue.Symbol,
            Amount = eventValue.Amount
        };
        _objectMapper.Map(context, index);
        await _transferredRepository.AddOrUpdateAsync(index);
    }
}