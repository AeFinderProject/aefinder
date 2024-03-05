using AElfIndexer.App.Handlers;
using AElfIndexer.Sdk;
using AutoMapper;

namespace AElfIndexer.App;

public class AElfIndexerAppTestAutoMapperProfile : Profile
{
    public AElfIndexerAppTestAutoMapperProfile()
    {
        CreateMap<Sdk.Block, TestBlockEntity>();
        CreateMap<Transaction, TestTransactionEntity>();
    }
}