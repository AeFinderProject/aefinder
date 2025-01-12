using Volo.Abp.Application.Dtos;

namespace AeFinder.GraphQL.Dto;

public class IndexerUserFundRecordDto
{
    public PagedResultDto<UserFundRecordDto> UserFundRecord { get; set; }
}