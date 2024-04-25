using AeFinder.App.MockApp;
using AeFinder.Sdk.Processor;
using AutoMapper;

namespace AeFinder.App;

public class AeFinderAppTestAutoMapperProfile : Profile
{
    public AeFinderAppTestAutoMapperProfile()
    {
        CreateMap<Sdk.Processor.Block, BlockEntity>();
        CreateMap<Transaction, TransactionEntity>();
    }
}