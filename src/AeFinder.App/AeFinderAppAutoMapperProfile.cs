using AeFinder.Block.Dtos;
using AeFinder.Sdk;
using AeFinder.Sdk.Processor;
using AutoMapper;

namespace AeFinder.App;

public class AeFinderAppAutoMapperProfile:Profile
{
    public AeFinderAppAutoMapperProfile()
    {
        CreateMap<BlockWithTransactionDto, Sdk.Processor.Block>();
        CreateMap<BlockWithTransactionDto, LightBlock>();
        CreateMap<TransactionDto, Transaction>();
        CreateMap<LogEventDto, LogEvent>();
        CreateMap<BlockWithTransactionDto, BlockMetadata>();
    }
}