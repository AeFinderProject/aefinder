using AeFinder.Block.Dtos;
using AeFinder.Entities.Es;
using AeFinder.Etos;
using AutoMapper;

namespace AeFinder;

public class AeFinderApplicationAutoMapperProfile:Profile
{
    public AeFinderApplicationAutoMapperProfile()
    {
        CreateMap<BlockIndex,BlockDto>();
        CreateMap<Transaction, TransactionDto>();
        CreateMap<LogEvent, LogEventDto>();

        CreateMap<TransactionIndex, TransactionDto>();
        CreateMap<LogEventIndex, LogEventDto>();
        
        CreateMap<BlockDto,BlockWithTransactionDto>();
        CreateMap<NewBlockEto,BlockWithTransactionDto>();
        CreateMap<ConfirmBlockEto,BlockWithTransactionDto>();
    }
    
}