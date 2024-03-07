using System;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElfIndexer.Sdk;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.App.MockPlugin;

public class TokenTransferredProcessor : LogEventProcessorBase<Transferred>, ITransientDependency
{
    public override string GetContractAddress(string chainId)
    {
        return "TokenContractAddress";
    }

    public override async Task ProcessAsync(Transferred logEvent, LogEventContext context)
    {
        if (context.Block.BlockHeight == 100000)
        {
            throw new Exception();
        }

        var transfer = new TransferEntity
        {
            Id = context.Transaction.TransactionId + context.LogEvent.Index + logEvent.Amount,
            FromAccount = logEvent.From.ToBase58(),
            ToAccount = logEvent.To.ToBase58(),
            Symbol = logEvent.Symbol,
            Amount = logEvent.Amount
        };
        await SaveEntityAsync(transfer);
        
        
        if(context.Block.BlockHash == "BlockHashA109")
        {
            var account = await GetEntityAsync<AccountBalanceEntity>(logEvent.From.ToBase58());
            if(account != null)
            {
                await DeleteEntityAsync(account);
            }
        }
        else
        {
            await ChangeBalanceAsync(logEvent.From.ToBase58(), logEvent.Symbol, -logEvent.Amount);
        }
        await ChangeBalanceAsync(logEvent.To.ToBase58(), logEvent.Symbol, logEvent.Amount);
    }
    
    private async Task ChangeBalanceAsync(string address, string symbol, long amount)
    {
        var account = await GetEntityAsync<AccountBalanceEntity>(address);
        if (account == null)
        {
            account = new AccountBalanceEntity
            {
                Id = address,
                Symbol = symbol,
                Amount = amount,
                Account = address
            };
        }
        else
        {
            account.Amount += amount;
        }

        await SaveEntityAsync(account);
    }
}