using AElfScan.AElf.Dtos;
using AElfScan.AElf.Entities.Es;
using AElfScan.AElf.Etos;
using AutoMapper;

namespace AElfScan.AElf;

public class AElfAutoMapperProfile:Profile
{
    public AElfAutoMapperProfile()
    {
        CreateMap<ConfirmBlockEto,Block>();
        CreateMap<Block,ConfirmBlocksEto>();
        CreateMap<ConfirmBlocksEto,BlockDto>();
    }
}