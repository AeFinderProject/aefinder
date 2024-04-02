using AeFinder.Block.Dtos;
using AeFinder.BlockChainEventHandler.DTOs;
using AutoMapper;

namespace AeFinder.BlockGrainManager;

public class AeFinderBlockGrainManagerAutoMapperProfile:Profile
{
    public AeFinderBlockGrainManagerAutoMapperProfile()
    {
        CreateMap<BlockDto, BlockEto>();
        CreateMap<TransactionDto, TransactionEto>();
        CreateMap<LogEventDto, LogEventEto>();
    }
}