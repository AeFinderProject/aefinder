using AElfIndexer.Etos;
using AutoMapper;

namespace AElfIndexer.EntityEventHandler.Core.Tests.AElf;

public class AElfAutoMapperProfile:Profile
{
    public AElfAutoMapperProfile()
    {
        CreateMap<NewBlockEto, ConfirmBlockEto>();
    }
    
}