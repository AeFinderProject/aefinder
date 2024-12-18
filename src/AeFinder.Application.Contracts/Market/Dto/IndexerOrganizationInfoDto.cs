using Volo.Abp.Application.Dtos;

namespace AeFinder.Market;

public class IndexerOrganizationInfoDto
{
    public PagedResultDto<MemberOrganizationDto> Organization { get; set; }
}