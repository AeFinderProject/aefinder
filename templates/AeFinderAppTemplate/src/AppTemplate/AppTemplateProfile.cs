using AppTemplate.Entities;
using AppTemplate.GraphQL;
using AutoMapper;

namespace AppTemplate;

public class AppTemplateProfile : Profile
{
    public AppTemplateProfile()
    {
        CreateMap<MyEntity, MyEntityDto>();
    }
}