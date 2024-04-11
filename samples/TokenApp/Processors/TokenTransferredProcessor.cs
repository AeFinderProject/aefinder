using AElf.Contracts.MultiToken;
using AeFinder.Sdk.Logging;
using AeFinder.Sdk.Processor;
using TokenApp.Entities;
using Volo.Abp.DependencyInjection;

namespace TokenApp.Processors;

public class TokenTransferredProcessor : LogEventProcessorBase<Transferred>, ITransientDependency
{
    public override string GetContractAddress(string chainId)
    {
        return chainId switch
        {
            "AELF" => "JRmBduh4nXWi1aXgdUsj5gJrzeZb2LxmrAbf7W99faZSvoAaE",
            "tDVW" => "ASh2Wt7nSEmYqnGxPPzp4pnVDU4uhj1XW9Se5VeZcX2UDdyjx",
            _ => throw new Exception("Unknown chain id")
        };
    }

    public override async Task ProcessAsync(Transferred logEvent, LogEventContext context)
    {
        var transfer = new TransferRecord
        {
            Id = context.ChainId + "-" + context.Transaction.TransactionId,
            FromAddress = logEvent.From.ToBase58(),
            ToAddress = logEvent.To.ToBase58(),
            Symbol = logEvent.Symbol,
            Amount = logEvent.Amount
        };
        await SaveEntityAsync(transfer);
        
        await ChangeBalanceAsync(context.ChainId, logEvent.From.ToBase58(), logEvent.Symbol, -logEvent.Amount);
        await ChangeBalanceAsync(context.ChainId, logEvent.To.ToBase58(), logEvent.Symbol, logEvent.Amount);
    }

    private async Task ChangeBalanceAsync(string chainId, string address, string symbol, long amount)
    {
        var accountId = chainId + "-" + address;
        var account = await GetEntityAsync<Account>(accountId);
        if (account == null)
        {
            account = new Account
            {
                Id = accountId,
                Symbol = symbol,
                Amount = amount,
                Address = address
            };
        }
        else
        {
            account.Amount += amount;
        }

        Logger.LogDebug("Balance changed: {0} {1} {2}", account.Address, account.Symbol, account.Amount);
        
        await SaveEntityAsync(account);
    }
}