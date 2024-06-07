using AeFinder.Block.Dtos;
using AeFinder.BlockScan;
using AeFinder.Sdk.Dtos;
using AeFinder.Sdk.Entities;
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
        
        CreateMap<Metadata, MetadataDto>();
        CreateMap<BlockMetadata, BlockMetadataDto>();
        
        CreateMap<AppSubscribedBlockDto, BlockWithTransactionDto>();
        CreateMap<AppSubscribedTransactionDto, TransactionDto>();
        CreateMap<AppSubscribedLogEventDto, LogEventDto>();
    }
}