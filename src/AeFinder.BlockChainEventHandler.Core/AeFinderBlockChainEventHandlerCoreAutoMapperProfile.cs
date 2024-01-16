using AeFinder.BlockChainEventHandler.Core.DTOs;
using AeFinder.Etos;
using AeFinder.Grains.EventData;
using AutoMapper;

namespace AeFinder.BlockChainEventHandler.Core;

public class AeFinderBlockChainEventHandlerCoreAutoMapperProfile:Profile
{
    public AeFinderBlockChainEventHandlerCoreAutoMapperProfile()
    {
        /* You can configure your AutoMapper mapping configuration here.
         * Alternatively, you can split your mapping configurations
         * into multiple profile classes for a better organization. */
        CreateMap<BlockEto,NewBlockEto>();
        CreateMap<TransactionEto, AeFinder.Entities.Es.Transaction>();
        CreateMap<LogEventEto, AeFinder.Entities.Es.LogEvent>();

        CreateMap<NewBlockEto, BlockData>();
        CreateMap<AeFinder.Entities.Es.Transaction, Transaction>();
        CreateMap<AeFinder.Entities.Es.LogEvent, LogEvent>();
        
        CreateMap<BlockData, ConfirmBlockEto>();
        CreateMap<Transaction, AeFinder.Entities.Es.Transaction>();
        CreateMap<LogEvent, AeFinder.Entities.Es.LogEvent>();
    }
}