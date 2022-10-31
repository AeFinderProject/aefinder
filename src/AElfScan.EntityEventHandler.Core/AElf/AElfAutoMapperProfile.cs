using AElfScan.AElf.Entities.Es;
using AElfScan.AElf.Etos;
using AutoMapper;

namespace AElfScan.AElf;

public class AElfAutoMapperProfile:Profile
{
    public AElfAutoMapperProfile()
    {
        CreateMap<ConfirmBlockEto,BlockIndex>();
    }
}