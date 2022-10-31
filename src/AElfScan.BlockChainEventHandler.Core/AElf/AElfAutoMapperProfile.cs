using AElfScan.AElf.DTOs;
using AElfScan.AElf.Entities.Es;
using AElfScan.AElf.Etos;
using AElfScan.EventData;
using AutoMapper;
using Volo.Abp.AutoMapper;

namespace AElfScan.AElf;

public class AElfAutoMapperProfile:Profile
{
    public AElfAutoMapperProfile()
    {
        /* You can configure your AutoMapper mapping configuration here.
         * Alternatively, you can split your mapping configurations
         * into multiple profile classes for a better organization. */
        CreateMap<BlockEto,NewBlockEto>();
        CreateMap<TransactionEto, Transaction>();
        CreateMap<LogEventEto, LogEvent>();

        CreateMap<Transaction, NewTransactionEto>().Ignore(x => x.Id);
        CreateMap<LogEvent, NewLogEventEto>().Ignore(x => x.Id);

        CreateMap<NewBlockEto, BlockEventData>();
        
        CreateMap<BlockEventData, ConfirmBlockEto>();
        CreateMap<Transaction, ConfirmTransactionEto>().Ignore(x => x.Id);
        CreateMap<LogEvent, ConfirmLogEventEto>().Ignore(x => x.Id);
    }
}