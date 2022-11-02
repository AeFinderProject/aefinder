using AElfScan.AElf;
using AElfScan.AElf.Dtos;

namespace AElfScan.Grains.Grain.BlockScan;

public class BlockFilterProvider : IBlockFilterProvider
{
    private readonly IBlockAppService _blockAppService;

    public BlockFilterType FilterType { get; } = BlockFilterType.Block;

    public BlockFilterProvider(IBlockAppService blockAppService)
    {
        _blockAppService = blockAppService;
    }

    public async Task<List<BlockDto>> GetBlocksAsync(string chainId, long startBlockNumber, long endBlockNumber,
        bool onlyConfirmed, List<FilterContractEventInput> filters)
    {
        var blocks = await _blockAppService.GetBlocksAsync(new GetBlocksInput
        {
            ChainId = chainId,
            HasTransaction = true,
            StartBlockNumber = startBlockNumber,
            EndBlockNumber = endBlockNumber,
            IsOnlyConfirmed = onlyConfirmed,
            Events = filters
        });

        return blocks;
    }

    public async Task<List<BlockDto>> FilterBlocksAsync(List<BlockDto> blocks, List<FilterContractEventInput> filters)
    {
        if (filters == null || filters.Count == 0)
        {
            return blocks;
        }

        var contractAddressFilter = new HashSet<string>();
        var logEventFilter = new HashSet<string>();
        foreach (var filter in filters)
        {
            if (filter.EventNames == null || filter.EventNames.Count == 0)
            {
                contractAddressFilter.Add(filter.ContractAddress);
            }
            else
            {
                foreach (var eventName in filter.EventNames)
                {
                    logEventFilter.Add(filter.ContractAddress + eventName);
                }
            }
        }

        return (from block in blocks
            from transaction in block.Transactions
            from logEvent in transaction.LogEvents
            where (contractAddressFilter.Count > 0 &&
                   contractAddressFilter.Contains(logEvent.ContractAddress)) ||
                  (logEventFilter.Count > 0 &&
                   logEventFilter.Contains(logEvent.ContractAddress + logEvent.EventName))
            select block).ToList();
    }
}