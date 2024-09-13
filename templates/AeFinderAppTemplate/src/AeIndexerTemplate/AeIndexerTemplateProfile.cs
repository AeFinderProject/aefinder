using AeIndexerTemplate.Entities;
using AeIndexerTemplate.GraphQL;
using AutoMapper;

namespace AeIndexerTemplate;

public class AeIndexerTemplateProfile : Profile
{
    public AeIndexerTemplateProfile()
    {
        CreateMap<MyEntity, MyEntityDto>();
    }
}