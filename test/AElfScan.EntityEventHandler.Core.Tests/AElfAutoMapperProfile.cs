using AElfScan.Etos;
using AutoMapper;

namespace AElfScan.EntityEventHandler.Core.Tests.AElf;

public class AElfAutoMapperProfile:Profile
{
    public AElfAutoMapperProfile()
    {
        CreateMap<NewBlockEto, ConfirmBlockEto>();
    }
    
}