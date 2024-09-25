using AeIndexerTemplate.Entities;
using AeIndexerTemplate.GraphQL;
using AutoMapper;

namespace AeIndexerTemplate;

public class AeIndexerTemplateAutoMapperProfile : Profile
{
    public AeIndexerTemplateAutoMapperProfile()
    {
        CreateMap<MyEntity, MyEntityDto>();
    }
}