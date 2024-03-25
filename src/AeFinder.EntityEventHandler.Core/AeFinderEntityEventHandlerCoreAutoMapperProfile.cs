using AeFinder.Block.Dtos;
using AeFinder.Entities.Es;
using AeFinder.Etos;
using AutoMapper;
using Volo.Abp.AutoMapper;

namespace AeFinder.EntityEventHandler;

public class AeFinderEntityEventHandlerCoreAutoMapperProfile:Profile
{
    public AeFinderEntityEventHandlerCoreAutoMapperProfile()
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