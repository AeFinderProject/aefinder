using AElfIndexer.App.Handlers;
using AElfIndexer.App.MockPlugin;
using AElfIndexer.Sdk;
using AutoMapper;

namespace AElfIndexer.App;

public class AElfIndexerAppTestAutoMapperProfile : Profile
{
    public AElfIndexerAppTestAutoMapperProfile()
    {
        CreateMap<Sdk.Block, BlockEntity>();
        CreateMap<Transaction, TransactionEntity>();
    }
}