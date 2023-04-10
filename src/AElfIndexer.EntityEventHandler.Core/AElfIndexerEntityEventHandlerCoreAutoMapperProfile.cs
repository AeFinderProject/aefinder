using AElfIndexer.Block.Dtos;
using AElfIndexer.Entities.Es;
using AElfIndexer.Etos;
using AutoMapper;
using Volo.Abp.AutoMapper;

namespace AElfIndexer.AElf;

public class AElfIndexerEntityEventHandlerCoreAutoMapperProfile:Profile
{
    public AElfIndexerEntityEventHandlerCoreAutoMapperProfile()
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