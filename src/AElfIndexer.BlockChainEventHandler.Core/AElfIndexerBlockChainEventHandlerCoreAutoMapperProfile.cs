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
        CreateMap<TransactionEto, AElfIndexer.Entities.Es.Transaction>();
        CreateMap<LogEventEto, AElfIndexer.Entities.Es.LogEvent>();

        CreateMap<NewBlockEto, BlockData>();
        CreateMap<AElfIndexer.Entities.Es.Transaction, AElfIndexer.Grains.EventData.Transaction>();
        CreateMap<AElfIndexer.Entities.Es.LogEvent, AElfIndexer.Grains.EventData.LogEvent>();
        
        CreateMap<BlockData, ConfirmBlockEto>();
        CreateMap<AElfIndexer.Grains.EventData.Transaction, AElfIndexer.Entities.Es.Transaction>();
        CreateMap<AElfIndexer.Grains.EventData.LogEvent, AElfIndexer.Entities.Es.LogEvent>();
    }
}