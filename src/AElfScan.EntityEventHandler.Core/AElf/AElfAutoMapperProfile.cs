using AElfScan.AElf.Dtos;
using AElfScan.AElf.Entities.Es;
using AElfScan.AElf.Etos;
using AutoMapper;
using Volo.Abp.AutoMapper;

namespace AElfScan.AElf;

public class AElfAutoMapperProfile:Profile
{
    public AElfAutoMapperProfile()
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