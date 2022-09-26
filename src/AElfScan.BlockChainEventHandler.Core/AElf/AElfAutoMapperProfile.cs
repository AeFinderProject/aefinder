using AElfScan.AElf.DTOs;
using AElfScan.AElf.Entities.Es;
using AElfScan.AElf.Etos;
using AutoMapper;

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

        CreateMap<AElfScan.State.Block, ConfirmBlockEto>();
    }
}