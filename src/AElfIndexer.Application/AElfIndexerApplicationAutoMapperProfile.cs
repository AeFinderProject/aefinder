using AElfIndexer.Block.Dtos;
using AElfIndexer.Entities.Es;
using AutoMapper;
using Volo.Abp.AutoMapper;

namespace AElfIndexer;

public class AElfIndexerApplicationAutoMapperProfile:Profile
{
    public AElfIndexerApplicationAutoMapperProfile()
    {
        CreateMap<BlockIndex,BlockDto>();
        CreateMap<Transaction, TransactionDto>();
        CreateMap<LogEvent, LogEventDto>();

        CreateMap<TransactionIndex, TransactionDto>();
        CreateMap<LogEventIndex, LogEventDto>();
    }
    
}