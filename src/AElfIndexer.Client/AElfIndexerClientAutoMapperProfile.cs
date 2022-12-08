using AElfIndexer.Block.Dtos;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using AutoMapper;

namespace AElfIndexer.Client;

public class AElfIndexerClientAutoMapperProfile:Profile
{
    public AElfIndexerClientAutoMapperProfile()
    {
        CreateMap<LogEventDto, LogEventInfo>();
        CreateMap<BlockWithTransactionDto, BlockInfo>();
        CreateMap<TransactionInfo, LogEventContext>();
    }
    
}