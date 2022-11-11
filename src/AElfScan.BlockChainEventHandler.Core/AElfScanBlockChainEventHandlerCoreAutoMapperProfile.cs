using AElfScan.DTOs;
using AElfScan.Entities.Es;
using AElfScan.Etos;
using AElfScan.Grains.EventData;
using AutoMapper;
using Volo.Abp.AutoMapper;

namespace AElfScan;

public class AElfScanBlockChainEventHandlerCoreAutoMapperProfile:Profile
{
    public AElfScanBlockChainEventHandlerCoreAutoMapperProfile()
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