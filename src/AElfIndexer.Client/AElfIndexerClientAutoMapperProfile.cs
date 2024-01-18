using AElfIndexer.Block.Dtos;
using AutoMapper;

namespace AElfIndexer.Client;

public class AElfIndexerClientAutoMapperProfile:Profile
{
    public AElfIndexerClientAutoMapperProfile()
    {
        CreateMap<BlockWithTransactionDto, Sdk.Block>();
        CreateMap<BlockWithTransactionDto, Sdk.LightBlock>();
        CreateMap<TransactionDto, Sdk.Transaction>();
        CreateMap<LogEventDto, Sdk.LogEvent>();
        CreateMap<BlockWithTransactionDto, Sdk.BlockMetadata>();
    }
}