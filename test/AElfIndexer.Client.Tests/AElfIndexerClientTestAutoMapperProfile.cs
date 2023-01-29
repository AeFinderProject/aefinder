using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using AElfIndexer.Handler;
using AutoMapper;

namespace AElfIndexer;

public class AElfIndexerClientTestAutoMapperProfile : Profile
{
    public AElfIndexerClientTestAutoMapperProfile()
    {
        CreateMap<BlockInfo, TestBlockIndex>();
        CreateMap<TransactionInfo, TestTransactionIndex>();
        CreateMap<LogEventContext, TestTransferredIndex>();
    }
}