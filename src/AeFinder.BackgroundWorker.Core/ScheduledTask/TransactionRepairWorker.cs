using System.Collections.Concurrent;
using AeFinder.BackgroundWorker.Options;
using AeFinder.Entities.Es;
using AElf.EntityMapping.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AeFinder.BackgroundWorker.ScheduledTask;

public class TransactionRepairWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IEntityMappingRepository<BlockIndex, string> _blockIndexRepository;
    private readonly IEntityMappingRepository<TransactionIndex, string> _transactionIndexRepository;
    private readonly TransactionRepairOptions _transactionRepairOptions;

    private ConcurrentDictionary<string, long> _prcessedHeight;

    public TransactionRepairWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IEntityMappingRepository<TransactionIndex, string> transactionIndexRepository,
        IEntityMappingRepository<BlockIndex, string> blockIndexRepository,
        IOptionsSnapshot<TransactionRepairOptions> transactionRepairOptions) : base(timer,
        serviceScopeFactory)
    {
        _transactionIndexRepository = transactionIndexRepository;
        _blockIndexRepository = blockIndexRepository;
        _transactionRepairOptions = transactionRepairOptions.Value;

        Timer.Period = _transactionRepairOptions.Period;
        _prcessedHeight = new();
        foreach (var chain in _transactionRepairOptions.Chains)
        {
            _prcessedHeight[chain.Key] = chain.Value.StartBlockHeight - 1;
        }
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var task = _transactionRepairOptions.Chains.Select(o => RepairAsync(o.Key));
        await task.WhenAll();
    }

    private async Task RepairAsync(string chainId)
    {
        if (_prcessedHeight[chainId] >= _transactionRepairOptions.Chains[chainId].EndBlockHeight)
        {
            return;
        }

        if (_prcessedHeight[chainId] == -1)
        {
            return;
        }

        var startBlockHeight = _prcessedHeight[chainId] + 1;
        var endBlockHeight = _prcessedHeight[chainId] + _transactionRepairOptions.MaxBlockCount;

        var getBlocksTask = GetBlocksAsync(chainId, startBlockHeight, endBlockHeight);
        var getTransactionsTask = GetTransactionsAsync(chainId, startBlockHeight, endBlockHeight);
        var blocks = await getBlocksTask;
        var transactions = await getTransactionsTask;

        var toUpdateTxs = new List<TransactionIndex>();
        foreach (var block in blocks)
        {
            var index = 0;
            foreach (var txId in block.TransactionIds)
            {
                if (!transactions.TryGetValue(txId, out var tx))
                {
                    Logger.LogError(
                        "Cannot find transaction: {TxId}. ChainId: {ChainId}, BlockHash: {BlockHash}, BlockHeight: {BlockHeight}",
                        txId, chainId, block.BlockHash, block.BlockHeight);
                    _prcessedHeight[chainId] = -1;
                    return;
                }

                tx.Index = index;
                toUpdateTxs.Add(tx);
                index++;
            }

            _prcessedHeight[chainId] = block.BlockHeight;
        }

        await _transactionIndexRepository.AddOrUpdateManyAsync(toUpdateTxs);
        Logger.LogDebug("Processing success! ChainId: {ChainId}, Height: {Height}", chainId, _prcessedHeight[chainId]);
    }

    private async Task<List<BlockIndex>> GetBlocksAsync(string chainId, long startBlockHeight, long endBlockHeight)
    {
        var quaryable = await _blockIndexRepository.GetQueryableAsync();
        return quaryable.Where(o =>
                o.BlockHeight >= startBlockHeight && o.BlockHeight <= endBlockHeight && o.ChainId == chainId && o.Confirmed)
            .OrderBy(o => o.BlockHeight).Take(10000).ToList();
    }
    
    private async Task<Dictionary<string, TransactionIndex>> GetTransactionsAsync(string chainId, long startBlockHeight, long endBlockHeight)
    {
        var quaryable = await _transactionIndexRepository.GetQueryableAsync();
        var transactions = quaryable.Where(o =>
                o.BlockHeight >= startBlockHeight && o.BlockHeight <= endBlockHeight && o.ChainId == chainId && o.Confirmed)
            .OrderBy(o => o.BlockHeight).Take(20000).ToList();

        if (transactions.Count == 20000)
        {
            transactions = quaryable.Where(o =>
                    o.BlockHeight >= startBlockHeight && o.BlockHeight <= endBlockHeight && o.ChainId == chainId && o.Confirmed)
                .OrderBy(o => o.BlockHeight).Take(int.MaxValue).ToList();
        }

        return transactions.ToDictionary(o => o.TransactionId, o => o);
    }
}