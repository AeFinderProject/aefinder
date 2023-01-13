using AElfIndexer.Grains.State.Client;
using AElfIndexer.Handlers;
using AutoMapper;

namespace AElfIndexer;

public class AElfIndexerClientTestAutoMapperProfile:Profile
{
    public AElfIndexerClientTestAutoMapperProfile()
    {
        CreateMap<BlockInfo, TestBlockIndex>();
    }
    
}