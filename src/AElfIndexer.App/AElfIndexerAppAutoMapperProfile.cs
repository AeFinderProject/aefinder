using AElfIndexer.Block.Dtos;
using AutoMapper;

namespace AElfIndexer.App;

public class AElfIndexerAppAutoMapperProfile:Profile
{
    public AElfIndexerAppAutoMapperProfile()
    {
        CreateMap<BlockWithTransactionDto, Sdk.Block>();
        CreateMap<BlockWithTransactionDto, Sdk.LightBlock>();
        CreateMap<TransactionDto, Sdk.Transaction>();
        CreateMap<LogEventDto, Sdk.LogEvent>();
        CreateMap<BlockWithTransactionDto, Sdk.BlockMetadata>();
    }
}