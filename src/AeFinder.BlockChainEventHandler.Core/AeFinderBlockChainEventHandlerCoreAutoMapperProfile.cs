using AeFinder.BlockChainEventHandler.DTOs;
using AeFinder.Etos;
using AeFinder.Grains.EventData;
using AutoMapper;
using LogEvent = AeFinder.Entities.Es.LogEvent;
using Transaction = AeFinder.Entities.Es.Transaction;

namespace AeFinder.BlockChainEventHandler;

public class AeFinderBlockChainEventHandlerCoreAutoMapperProfile:Profile
{
    public AeFinderBlockChainEventHandlerCoreAutoMapperProfile()
    {
        /* You can configure your AutoMapper mapping configuration here.
         * Alternatively, you can split your mapping configurations
         * into multiple profile classes for a better organization. */
        CreateMap<BlockEto,NewBlockEto>();
        CreateMap<TransactionEto, Transaction>();
        CreateMap<LogEventEto, LogEvent>();

        CreateMap<NewBlockEto, BlockData>();
        CreateMap<Transaction, AeFinder.Grains.EventData.Transaction>();
        CreateMap<LogEvent, AeFinder.Grains.EventData.LogEvent>();
        
        CreateMap<BlockData, ConfirmBlockEto>();
        CreateMap<AeFinder.Grains.EventData.Transaction, Transaction>();
        CreateMap<AeFinder.Grains.EventData.LogEvent, LogEvent>();
    }
}