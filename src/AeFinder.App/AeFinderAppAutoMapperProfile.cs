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
        CreateMap<AppSubscribedBlockDto, Sdk.Processor.Block>();
        CreateMap<AppSubscribedBlockDto, LightBlock>();
        CreateMap<AppSubscribedTransactionDto, Transaction>();
        CreateMap<AppSubscribedLogEventDto, LogEvent>();
        CreateMap<AppSubscribedBlockDto, BlockMetadata>();
        
        CreateMap<Metadata, MetadataDto>();
        CreateMap<BlockMetadata, BlockMetadataDto>();
    }
}