using AeFinder.Block.Dtos;
using AeFinder.Client.Handlers;
using AeFinder.Grains.State.Client;
using AutoMapper;

namespace AeFinder.Client;

public class AeFinderClientAutoMapperProfile:Profile
{
    public AeFinderClientAutoMapperProfile()
    {
        CreateMap<LogEventDto, LogEventInfo>();
        CreateMap<BlockWithTransactionDto, BlockInfo>();
        CreateMap<TransactionInfo, LogEventContext>();
        CreateMap<TransactionDto, TransactionInfo>();
    }
    
}