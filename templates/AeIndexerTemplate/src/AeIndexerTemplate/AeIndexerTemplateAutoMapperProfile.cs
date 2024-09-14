using AeIndexerTemplate.Entities;
using AeIndexerTemplate.GraphQL;
using AutoMapper;

namespace AeIndexerTemplate;

public class AeIndexerTemplateAutoMapperProfile : Profile
{
    public AeIndexerTemplateProfile()
    {
        CreateMap<MyEntity, MyEntityDto>();
    }
}