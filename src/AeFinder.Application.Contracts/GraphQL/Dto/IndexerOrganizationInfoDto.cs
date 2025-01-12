using Volo.Abp.Application.Dtos;

namespace AeFinder.GraphQL.Dto;

public class IndexerOrganizationInfoDto
{
    public PagedResultDto<MemberOrganizationDto> Organization { get; set; }
}