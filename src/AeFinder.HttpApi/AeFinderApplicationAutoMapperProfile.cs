using AeFinder.Apps.Dto;
using AeFinder.Models;
using AutoMapper;

namespace AeFinder;

public class AeFinderHttpApiAutoMapperProfile : Profile
{
    public AeFinderHttpApiAutoMapperProfile()
    {
        CreateMap<SetAppResourceLimitsInput, SetAppResourceLimitDto>();
    }
}