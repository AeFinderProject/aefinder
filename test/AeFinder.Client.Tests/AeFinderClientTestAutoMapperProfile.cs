using AeFinder.Client.Handlers;
using AeFinder.Grains.State.Client;
using AutoMapper;

namespace AeFinder.Client;

public class AeFinderClientTestAutoMapperProfile : Profile
{
    public AeFinderClientTestAutoMapperProfile()
    {
        CreateMap<BlockInfo, TestBlockIndex>();
        CreateMap<TransactionInfo, TestTransactionIndex>();
        CreateMap<LogEventContext, TestTransferredIndex>();
        CreateMap<BlockInfo, TestIndex>();
    }
}