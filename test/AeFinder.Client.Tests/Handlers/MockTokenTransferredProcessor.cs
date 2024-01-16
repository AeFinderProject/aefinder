using System.Threading.Tasks;
using AeFinder.Grains.State.Client;
using AElf.Contracts.MultiToken;
using Microsoft.Extensions.Logging;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Client.Handlers;

public class MockTokenTransferredProcessor : AElfLogEventProcessorBase<Transferred, LogEventInfo>
{
    private readonly IAeFinderClientEntityRepository<TestTransferredIndex, LogEventInfo> _transferredRepository;
    private readonly IObjectMapper _objectMapper;

    public MockTokenTransferredProcessor(
        ILogger<AElfLogEventProcessorBase<Transferred, LogEventInfo>> logger,
        IObjectMapper objectMapper,
        IAeFinderClientEntityRepository<TestTransferredIndex, LogEventInfo> transferredRepository)
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