using AeFinder.Etos;
using AutoMapper;

namespace AeFinder.EntityEventHandler;

public class AeFinderAutoMapperProfile:Profile
{
    public AeFinderAutoMapperProfile()
    {
        CreateMap<NewBlockEto, ConfirmBlockEto>();
    }
    
}