using AElfIndexer.DTOs;
using AElfIndexer.Entities.Es;
using AElfIndexer.Etos;
using AElfIndexer.Grains.EventData;
using AutoMapper;
using Volo.Abp.AutoMapper;

namespace AElfIndexer;

public class AElfIndexerBlockChainEventHandlerCoreAutoMapperProfile:Profile
{
    public AElfIndexerBlockChainEventHandlerCoreAutoMapperProfile()
    {
        /* You can configure your AutoMapper mapping configuration here.
         * Alternatively, you can split your mapping configurations
         * into multiple profile classes for a better organization. */
        CreateMap<BlockEto,NewBlockEto>();
        CreateMap<TransactionEto, Transaction>();
        CreateMap<LogEventEto, LogEvent>();

        CreateMap<NewBlockEto, BlockEventData>();
        
        CreateMap<BlockEventData, ConfirmBlockEto>();
    }
}