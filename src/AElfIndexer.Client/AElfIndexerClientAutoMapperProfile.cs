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
        CreateMap<TransactionDto, TransactionInfo>();
        
        CreateMap<BlockWithTransactionDto, Sdk.Block>();
        CreateMap<BlockWithTransactionDto, Sdk.LightBlock>();
        CreateMap<TransactionDto, Sdk.Transaction>();
        CreateMap<LogEventDto, Sdk.LogEvent>();
    }
    
}