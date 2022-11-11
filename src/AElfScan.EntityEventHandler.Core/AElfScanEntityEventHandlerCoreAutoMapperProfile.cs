using AElfScan.Block.Dtos;
using AElfScan.Entities.Es;
using AElfScan.Etos;
using AutoMapper;
using Volo.Abp.AutoMapper;

namespace AElfScan.AElf;

public class AElfScanEntityEventHandlerCoreAutoMapperProfile:Profile
{
    public AElfScanEntityEventHandlerCoreAutoMapperProfile()
    {
        CreateMap<ConfirmBlockEto,BlockIndex>();
        CreateMap<BlockIndex,ConfirmBlocksEto>();
        CreateMap<ConfirmBlocksEto,BlockDto>();

        CreateMap<NewBlockEto, BlockIndex>();
        CreateMap<ConfirmBlockEto,BlockIndex>();

        CreateMap<Transaction, TransactionIndex>().Ignore(x => x.Id);
        CreateMap<LogEvent, LogEventIndex>().Ignore(x => x.Id);
    }
}